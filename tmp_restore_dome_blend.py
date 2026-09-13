import bpy
from pathlib import Path
root=Path(r'C:\UnityProject\NITROZEROtrailer\NITROZERO')
folder=root/'Assets/01RAEHYEON/industrialDome'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(folder/'industrial_dome.fbx'))
mesh=bpy.data.objects['IndustrialDome'].data
assert len(mesh.materials)==1
mat=mesh.materials[0]
mat.use_nodes=True
bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
for link in list(bsdf.inputs['Base Color'].links):mat.node_tree.links.remove(link)
tex=mat.node_tree.nodes.new('ShaderNodeTexImage')
tex.image=bpy.data.images.load(str(folder/'T_IndustrialDome_AgedPaint_Base.png'),check_existing=True)
mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
bsdf.inputs['Metallic'].default_value=0
bsdf.inputs['Roughness'].default_value=.76
bpy.ops.wm.save_as_mainfile(filepath=str(root/'SourceAssets/industrial_dome_cleaned.blend'))
print('RESTORED',len(mesh.vertices),len(mesh.polygons),[x.name for x in mesh.materials])
