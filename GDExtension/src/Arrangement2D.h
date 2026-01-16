#pragma once

#include "godot_cpp/classes/object.hpp"
#include "godot_cpp/classes/ref_counted.hpp"
#include <godot_cpp/templates/rid_owner.hpp>
#include "ArrangementAlias.h"

using namespace godot;

class Arrangement2D : public Object
{
    GDCLASS(Arrangement2D, Object)
protected:
    static void _bind_methods();
public:
    Arrangement2D() = default;
    CGAL::Arrangement Arrangement = {};
    CGAL::PointLocation PointLocation = { Arrangement };
    RID_Owner<CGAL::Curve_handle> CurveIDs;

    void _notification(int what);

    RID add(PackedVector2Array polyline);
    void free_rid(RID id);
    Array batch_query(PackedVector2Array points) const; // return array of PackedVector2Array

    static PackedVector2Array RemoveConsecutiveOverlappingPoint(PackedVector2Array polyline);
    static std::vector<CGAL::Point> Vector2Point(PackedVector2Array polyline);
    static PackedVector2Array Face2Vector(CGAL::Face_const_handle face);
    inline static const CGAL::Geom_traits::Construct_curve_2 CurveConstructor =
        CGAL::Geom_traits{}.construct_curve_2_object();
};