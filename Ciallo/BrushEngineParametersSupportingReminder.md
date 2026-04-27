# Brush engine paramemters supporting reminder

## Mypaint
**Totoal #brushes: 196**
| Can support | #Brush change it |
| --- | --- |
opaque_multiply | 196
hardness | 163
elliptical_dab_angle | 103
elliptical_dab_ratio | 78
dabs_per_actual_radius | 146

| Not sure || Note
|---|---|---|
offset_by_random(Jitter)|86|Will get outside of a stroke rim by setting it|
speed1_gamma(Fine Speed Gamma)|58|Not implement speed, editing stroke cause wired speed value
speed1_slowness(Gross Speed Gamma)|34|
stroke_duration_logarithmic|56|
slow_tracking|53|
direction_filter|42|Not sure what is this doing

speed1_gamma

| Cannot/Won't support |  #Brush change it | Note |
| --- | --- | --- |
opaque|149|Stroke-level opacity can never be supported, flow (stamp-level opacity) only.
opaque_linearize|102|
smudge|95| Whose algorithm need to read&write framebuffer dynamically
smudge_length|69|
dabs_per_basic_radius|75|dabs_per_actual_radius can totally replace this
dabs_per_second|45|
radius_by_random|37|
offset_by_speed|27|
