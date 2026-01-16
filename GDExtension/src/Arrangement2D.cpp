//
// Created by Ciao on 2026/1/16.
//

#include "Arrangement2D.h"

void Arrangement2D::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("add", "polyline"), &Arrangement2D::add);
    ClassDB::bind_method(D_METHOD("free_rid", "id"), &Arrangement2D::free_rid);
    ClassDB::bind_method(D_METHOD("batch_query", "points"), &Arrangement2D::batch_query);
}

void Arrangement2D::_notification(int what)
{
	if (what == NOTIFICATION_PREDELETE) {
		List<RID> rids;
		CurveIDs.get_owned_list(&rids);
		for (auto id : rids)
		{
			CurveIDs.free(id);
		}
	}
}

RID Arrangement2D::add(PackedVector2Array polyline)
{
    polyline = RemoveConsecutiveOverlappingPoint(polyline);
    CGAL::Curve curve = CurveConstructor(Vector2Point(polyline));
    auto handle = CGAL::insert(Arrangement, curve);
    return CurveIDs.make_rid(handle);
}

void Arrangement2D::free_rid(RID id)
{
    CGAL::Curve_handle* handle_ptr = CurveIDs.get_or_null(id);
    if (handle_ptr == nullptr)
    {
        print_error("Invalid Curve ID {}", id);
        return;
    }
    CGAL::remove_curve(Arrangement, *handle_ptr);
	CurveIDs.free(id);
}

Array Arrangement2D::batch_query(PackedVector2Array points) const
{
    Array polygons;
    polygons.resize(points.size());

    using Query_result = std::pair<CGAL::Point, CGAL::PointLocation::Result_type>;
    std::list<Query_result> queryResults;

    // CGAL::locate requires a linear container.
    std::vector<CGAL::Point> ps = Vector2Point(points);
    CGAL::locate(Arrangement, ps.begin(), ps.end(), std::back_inserter(queryResults));

    // Get points
    for (auto& [p, result] : queryResults) {
        size_t index = std::distance(ps.begin(), std::find(ps.begin(), ps.end(), p));
    	auto faceHandlePtr = std::get_if<CGAL::Face_const_handle>(&result);
        if (faceHandlePtr != nullptr && !(*faceHandlePtr)->is_unbounded())
	        polygons[index] = Face2Vector(*faceHandlePtr); // Face2Vector is ignoring holes
        else
        	polygons[index] = {};
    }
    return polygons;
}

PackedVector2Array Arrangement2D::RemoveConsecutiveOverlappingPoint(PackedVector2Array polyline)
{
    auto endIt = std::unique(polyline.begin(), polyline.end());
    size_t size = 0;
    auto it = polyline.begin();
    while (it != endIt)
    {
        size++;
        ++it;
    }
    polyline.resize(size);
    return polyline;
}

std::vector<CGAL::Point> Arrangement2D::Vector2Point(PackedVector2Array polyline)
{
    std::vector<CGAL::Point> points;
    points.reserve(polyline.size());
    for (auto p : polyline)
    {
        points.emplace_back(p.x, p.y);
    }
    return points;
}
/// <remarks>
/// If a line is inserted(pierced) into a face but not across it, CGAL will return vertices associated with this line.
/// Need to eliminate this pattern with a palindromic detection.
/// </remarks>
PackedVector2Array Arrangement2D::Face2Vector(CGAL::Face_const_handle face)
{
	std::vector<CGAL::Arrangement::Ccb_halfedge_const_circulator> ccb_circulators;
	ccb_circulators.push_back(face->outer_ccb());
	for (auto hole = face->holes_begin(); hole != face->holes_end(); ++hole)
	{
		// Don't deal with holes in current version
		// ccb_circulators.push_back(*hole);
	}

	for (auto& start_iterator : ccb_circulators)
	{
		// Remove palindromic halfEdges.
		auto curr = start_iterator;
		bool palindromic = false;

		std::vector<CGAL::Halfedge_const_handle> halfedge_stack{};
		do
		{
			if (!palindromic) // regular
			{
				halfedge_stack.push_back(curr);
			}

			if (curr->prev() == curr->twin()) // prev is the same as twin, so this is a "peek edge" of the palindromic
			{
				halfedge_stack.pop_back();
				palindromic = true;
			}

			if (palindromic)
			{
				if (!halfedge_stack.empty() && curr->twin() == halfedge_stack.back())
				{
					// do palindromic remove
					halfedge_stack.pop_back();
				}
				else // not palindromic anymore
				{
					palindromic = false;
					halfedge_stack.push_back(curr);
				}
			}
		} while (++curr != start_iterator);

		if (halfedge_stack.empty()) continue;

		// Get the points from halfedges.
		PackedVector2Array polygon = {};
		for (auto& halfedge : halfedge_stack)
		{
			// Points in halfedge->curve() is always in x-mono increasing order, may not begin from source and end to target, so we need to reverse some of them.
			// halfedge->source()->point() is the start point of the polyline
			// halfedge->target()->point() is the start point of the polyline
			// halfedge->curve().points_begin() can be both source or target

			// cannot use `halfEdge->curve().points_end()-1`, must use --halfedge->curve().points_end().
			auto beginIt = halfedge->curve().points_begin();

			if (halfedge->source()->point() == *beginIt)
			{
				for (auto it = beginIt; it != --halfedge->curve().points_end(); ++it)
				{
					Vector2 vec{
						static_cast<float>(CGAL::to_double(it->x())),
						static_cast<float>(CGAL::to_double(it->y()))
					};
					polygon.push_back(vec);
				}
			}
			else
			{
				// reverse iteration
				for (auto it = --halfedge->curve().points_end(); it != beginIt; --it)
				{
					Vector2 vec{
						static_cast<float>(CGAL::to_double(it->x())),
						static_cast<float>(CGAL::to_double(it->y())) };
					polygon.push_back(vec);
				}
			}
		}
		return polygon;
	}
	// Not reachable
	return {};
}
