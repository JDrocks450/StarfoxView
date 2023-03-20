#local skybox=2;
camera {location <10,10,-10> look_at<0,1,0>}
#if (skybox=1) sky_sphere { pigment {gradient y color_map { [0 rgb<1,1,1>] [1 rgb<0,0,1>] }scale 2 translate <0,-1,0>}}#end
#if (skybox=2)sky_sphere{pigment{ gradient <0,1,0>color_map{
[0.00 color rgb<0.24,0.32,1> *0.3]
[0.23 color rgb<0.16,0.32,0.9> *0.9]
[0.37 color rgb<1,0.1,0> ]
[0.52 color rgb<1,0.2,0> ]
[0.70 color rgb<0.36,0.32,1> *0.7 ]
[0.80 color rgb<0.14,0.32,1> *0.5 ]
[1.00 color rgb<0.24,0.32,1> *0.3 ]
}scale 2 rotate <0,0,0>translate <0,0.9,0>}}#end
light_source {<100,200,-200>rgb <1,1,1>}
global_settings{radiosity{count 100}#default { texture { finish { ambient 0 diffuse 1 }}} }

object {
#include "..\OUTPUT.txt"
finish {brilliance 1 phong 1 reflection {0.1}} 
rotate y*180
}