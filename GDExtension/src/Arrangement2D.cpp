//
// Created by Ciao on 2026/1/16.
//

#include "Arrangement2D.h"

void Arrangement2D::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("create_polyline"), &Arrangement2D::create_polyline);
    ClassDB::bind_method(D_METHOD("remove_polyline", "id"), &Arrangement2D::remove_polyline);
    ClassDB::bind_method(D_METHOD("set_polyline", "id", "data"), &Arrangement2D::set_polyline);
    ClassDB::bind_method(D_METHOD("query", "point"), &Arrangement2D::query);
	ClassDB::bind_method(D_METHOD("polyline_query", "polyline"), &Arrangement2D::polyline_query);
    ClassDB::bind_method(D_METHOD("batch_query", "points"), &Arrangement2D::batch_query);
    ClassDB::bind_method(D_METHOD("face_get_polygon", "id"), &Arrangement2D::face_get_polygon);
    ClassDB::bind_method(D_METHOD("face_is_unbounded", "id"), &Arrangement2D::face_is_unbounded);
}

void Arrangement2D::_notification(int what)
{
	if (what == NOTIFICATION_PREDELETE) {
		List<RID> rids;
		CurveHandleOwner.get_owned_list(&rids);
		for (auto id : rids)
		{
			CurveHandleOwner.free(id);
		}
		rids.clear();
		FaceHandleOwner.get_owned_list(&rids);
		for (auto id : rids)
		{
			FaceHandleOwner.free(id);
		}
	}
}

Arrangement2D::Arrangement2D()
{
	Observer.arr = this;
}

RID Arrangement2D::create_polyline()
{
    return CurveHandleOwner.make_rid({});
}

Array Arrangement2D::set_polyline(RID id, PackedVector2Array data)
{
	CGAL::Curve_handle* ptr = CurveHandleOwner.get_or_null(id);
	if (ptr == nullptr)
	{
		print_error(vformat("Given rid {} is not a polyline", id));
		return {};
	}

	CGAL::Curve_handle curve_handle = *ptr;
	if (curve_handle != nullptr)
		CGAL::remove_curve(Arrangement, curve_handle);

	if (data.size() == 0) return {};
	data = RemoveConsecutiveOverlappingPoint(data);
	CGAL::Curve curve = CurveConstructor(Vector2Point(data));
	auto handle = CGAL::insert(Arrangement, curve);
	*ptr = handle;

	Array result = InvalidFaceIDs;
	InvalidFaceIDs = Array();
	return result;
}

Array Arrangement2D::remove_polyline(RID id)
{
    CGAL::Curve_handle* ptr = CurveHandleOwner.get_or_null(id);
    if (ptr == nullptr)
    {
    	print_error(vformat("Given rid {} is not a polyline", id));
    	return {};
    }
    CurveHandleOwner.free(id);
    CGAL::Curve_handle curve_handle = *ptr;
	if (curve_handle != nullptr)
		CGAL::remove_curve(Arrangement, curve_handle);

	Array result = InvalidFaceIDs;
	InvalidFaceIDs = {};
	return result;
}

RID Arrangement2D::query(Vector2 p)
{
	auto obj = PointLocation.locate(CGAL::Point(p.x, p.y));
	auto faceHandlePtr = std::get_if<CGAL::Face_const_handle>(&obj);
	if (faceHandlePtr != nullptr)
	{
		CacheFaceHandle(*faceHandlePtr);
	}
	return {};
}

RID Arrangement2D::CacheFaceHandle(CGAL::Face_const_handle handle)
{
	if (FaceHandleToID.find(handle) == FaceHandleToID.end())
	{
		RID id = FaceHandleOwner.make_rid(handle);
		FaceHandleToID[handle] = id;
		return id;
	}
	else
	{
		return FaceHandleToID[handle];
	}
}

Array Arrangement2D::batch_query(PackedVector2Array points)
{
	Array rids{};
	rids.resize(points.size());

	using Query_result = std::pair<CGAL::Point, CGAL::PointLocation::Result_type>;
	std::list<Query_result> queryResults;

	// CGAL::locate requires a linear container.
	std::vector<CGAL::Point> ps = Vector2Point(points);
	CGAL::locate(Arrangement, ps.begin(), ps.end(), std::back_inserter(queryResults));

	// Get points
	for (auto& [p, obj] : queryResults) {
		size_t index = std::distance(ps.begin(), std::find(ps.begin(), ps.end(), p));
		auto faceHandlePtr = std::get_if<CGAL::Face_const_handle>(&obj);
		if (faceHandlePtr != nullptr)
			rids[index] = CacheFaceHandle(*faceHandlePtr);
	}
	return rids;
}

Array Arrangement2D::polyline_query(PackedVector2Array polyline)
{
	auto line = RemoveConsecutiveOverlappingPoint(polyline);
	if (line.size() == 0) return {};
	if (line.size() == 1) return { query(line[0]) };
	auto mono_curves = ConstructXMonotoneCurve(line);

	std::vector<RID> ids{};
	for (auto& curve : mono_curves)
	{
		auto face_handles = ZoneQuery(curve);
		for (auto handle : face_handles)
		{
			ids.push_back(CacheFaceHandle(handle));
		}
	}
	// remove duplicate
	Array result{};
	std::set s(ids.begin(), ids.end());
	for (RID id : s)
		result.push_back(id);
	return result;
}

std::vector<CGAL::X_monotone_curve> Arrangement2D::ConstructXMonotoneCurve(PackedVector2Array polyline)
{

	auto curve = CurveConstructor(Vector2Point(polyline));
	using Make_x_monotone_result = std::variant<CGAL::Point, CGAL::X_monotone_curve>;
	std::list<Make_x_monotone_result> result_objects;
	XMonoMaker(curve, std::back_inserter(result_objects));

	std::vector<CGAL::X_monotone_curve> result{};
	for (const auto& x_obj : result_objects)
	{
		const auto* mono_curve = std::get_if<CGAL::X_monotone_curve>(&x_obj);
		if (mono_curve != nullptr)
		{
			result.push_back(*mono_curve);
		}
	}
	return result;
}

std::vector<CGAL::Face_const_handle> Arrangement2D::ZoneQuery(const CGAL::X_monotone_curve& monoCurve)
{
	std::vector<CGAL::Face_const_handle> result;
	constexpr int MAX_RESULT = 256;
	using Result = std::variant<CGAL::Arrangement::Vertex_handle, CGAL::Arrangement::Halfedge_handle, CGAL::Arrangement::Face_handle>;
	std::vector<Result> output(MAX_RESULT);
	auto beginIt = output.begin();
	auto endIt = CGAL::zone(Arrangement, monoCurve, beginIt, PointLocation);
	for (auto it = beginIt; it != endIt; ++it)
	{
		if (auto faceHandlePtr = std::get_if<CGAL::Arrangement::Face_handle>(&*it))
		{
			result.emplace_back(*faceHandlePtr);
		}
	}

	return result;
}

PackedVector2Array Arrangement2D::face_get_polygon(RID id)
{
	if (!id.is_valid() || !FaceHandleOwner.owns(id)) return {};
	auto handle = *FaceHandleOwner.get_or_null(id);
	if (handle->is_unbounded()) return {};
	return Face2Vector(handle);
}

bool Arrangement2D::face_is_unbounded(RID id)
{
	if (!id.is_valid()) return false;
	if (!FaceHandleOwner.owns(id))
	{
		print_error(vformat("Given rid {} is not a face", id));
		return false;
	}
	CGAL::Face_const_handle handle = *FaceHandleOwner.get_or_null(id);
	return handle->is_unbounded();
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
/// Need to eliminate this pattern with palindromic detection.
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
