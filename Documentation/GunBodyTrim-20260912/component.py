import bpy
bpy.ops.wm.open_mainfile(filepath='C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912/inspection.blend')
o=bpy.data.objects['Body'];m=o.data
ps=[p for p in m.polygons if p.material_index==1]; adj={}
for p in ps:
 for i in p.vertices: adj.setdefault(i,set()).update(p.vertices)
seed=max(ps,key=lambda p:p.area).vertices[0];seen={seed};todo=[seed]
while todo:
 for i in adj[todo.pop()]:
  if i not in seen:seen.add(i);todo.append(i)
faces=[p.index for p in ps if p.vertices[0] in seen]
print('COMPONENT',len(seen),len(faces))
pts=[o.matrix_world@m.vertices[i].co for i in seen]
print('XYZ', [sorted(set(round(p[k],6) for p in pts))[:80] for k in range(3)])
import json
json.dump({'vertices':list(seen),'faces':faces},open('C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912/insert.json','w'))
