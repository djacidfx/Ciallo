#pragma once

#include "godot_cpp/classes/object.hpp"
#include "godot_cpp/classes/ref_counted.hpp"
#include <godot_cpp/templates/rid_owner.hpp>
#include "ArrangementAlias.h"
#include "ArrangementObserver.h"

using namespace godot;

class Arrangement2D : public Object
{
    GDCLASS(Arrangement2D, Object)

protected:
    static void _bind_methods();
public:
    CGAL::Arrangement Arrangement = {};
    CGAL::PointLocation PointLocation = { Arrangement };
    ArrangementObserver Observer{Arrangement};

    RID_Owner<CGAL::Curve_handle> CurveHandleOwner{};
    RID_Owner<Vector2> QueryPointOwner{};

    RID_Owner<CGAL::Face_const_handle> FaceHandleOwner{};
    std::unordered_map<CGAL::Face_const_handle, RID> FaceHandleToID{};
    Array InvalidFaceIDs = {}; // array of face handle rid

    void _notification(int what);

    Arrangement2D();

    RID create_polyline();
    Array set_polyline(RID id, PackedVector2Array data); // return invalid face RIDs
    Array remove_polyline(RID id);

    RID query(Vector2 point);
    Array batch_query(PackedVector2Array points);
    Array polyline_query(PackedVector2Array polyline);// return array of face RIDs
    PackedVector2Array face_get_polygon(RID id);
    bool face_is_unbounded(RID id);

    RID CacheFaceHandle(CGAL::Face_const_handle handle);
    std::vector<CGAL::Face_const_handle> ZoneQuery(const CGAL::X_monotone_curve& monoCurve);
    static std::vector<CGAL::X_monotone_curve> ConstructXMonotoneCurve(PackedVector2Array polyline);
    static PackedVector2Array RemoveConsecutiveOverlappingPoint(PackedVector2Array polyline);
    static std::vector<CGAL::Point> Vector2Point(PackedVector2Array polyline);
    static PackedVector2Array Face2Vector(CGAL::Face_const_handle face);
    inline static const CGAL::Geom_traits::Construct_curve_2 CurveConstructor =
        CGAL::Geom_traits{}.construct_curve_2_object();
    inline static const CGAL::Geom_traits::Make_x_monotone_2 XMonoMaker =
        CGAL::Geom_traits{}.make_x_monotone_2_object();
};
