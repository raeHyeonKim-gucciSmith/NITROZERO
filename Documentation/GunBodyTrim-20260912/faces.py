import bpy
bpy.ops.wm.open_mainfile(filepath='C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912/inspection.blend')
o=bpy.data.objects['Body'];m=o.data
ps=sorted([p for p in m.polygons if p.material_index==1],key=lambda p:p.area,reverse=True)
print('COUNTS',len(m.vertices),len(m.polygons),len(ps))
for p in ps[:18]:
 pts=[o.matrix_world@m.vertices[i].co for i in p.vertices]
 print('FACE',p.index,'N',len(pts),'AREA',p.area,'COORDS',[(round(v.x,4),round(v.y,4),round(v.z,4)) for v in pts[:20]])
