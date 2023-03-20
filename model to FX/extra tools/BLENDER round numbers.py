import bpy

#replace meshname with your mesh's name, note that is the MESH NAME and not the object name
#the mesh name can be found in the lil green triangle tab menu on the options on the right side of the screen
#this is also cap sensitive!
for vertex in bpy.data.meshes['meshname'].vertices:
    vertex.co[0] = round(vertex.co[0], 0)
    vertex.co[1] = round(vertex.co[1], 0)
    vertex.co[2] = round(vertex.co[2], 0)