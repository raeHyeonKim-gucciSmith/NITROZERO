import bpy,bmesh,json,math
from mathutils import Vector
from pathlib import Path
root=Path('C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912')
bpy.ops.wm.open_mainfile(filepath=str(root/'inspection.blend'))
body=bpy.data.objects['Body']; mesh=body.data
# Orthographic side preview, matching the user's marked contour.
for o in bpy.context.scene.objects:o.hide_render=o.name in ['Slide']
material_colors={m.name:tuple(m.diffuse_color) for m in bpy.data.materials}
for mat in bpy.data.materials:mat.diffuse_color=(.045,.05,.06,1) if 'Interior' in mat.name else (.48,.50,.53,1)
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH'
scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.18,.18,.18)
camdata=bpy.data.cameras.new('InspectionCamera');cam=bpy.data.objects.new('InspectionCamera',camdata);scene.collection.objects.link(cam)
cam.location=(0,-1.8,.72);target=Vector((0,0,.24));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=.79;scene.camera=cam
scene.render.resolution_x=1200;scene.render.resolution_y=750;scene.render.resolution_percentage=100
scene.render.filepath=str(root/'before.png');bpy.ops.render.render(write_still=True)
ids=set(json.load(open(root/'insert.json'))['vertices'])
bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[bm.verts[i] for i in ids],context='VERTS')
# Top follows the existing exterior cut: front rail, central recess, rear slope.
profile=[(-.300,.334),(-.204,.334),(-.157,.310),(-.009,.310),(.019,.331),(.146,.331),(.192,.301),(.300,.301),(.300,.280),(-.300,.280)]
inv=body.matrix_world.inverted();loops=[]
for y in [-.018,.018]:loops.append([bm.verts.new(inv@Vector((x,y,z))) for x,z in profile])
faces=[bm.faces.new(loops[0]),bm.faces.new(list(reversed(loops[1])))]
for i in range(len(profile)):
 j=(i+1)%len(profile);faces.append(bm.faces.new([loops[0][i],loops[1][i],loops[1][j],loops[0][j]]))
for f in faces:f.material_index=1
bmesh.ops.recalc_face_normals(bm,faces=faces);bmesh.ops.triangulate(bm,faces=faces)
bm.to_mesh(mesh);bm.free();mesh.update()
scene.render.filepath=str(root/'after.png');bpy.ops.render.render(write_still=True)
# Restore original material properties before exporting; preview colors are not asset edits.
for name,color in material_colors.items():bpy.data.materials[name].diffuse_color=color
# FBX embeds geometry and material names; Unity prefab retains its authored materials.
bpy.ops.object.select_all(action='DESELECT')
for name in ['Body','Slide','Trigger','Muzzle']:
 o=bpy.data.objects[name];o.hide_render=False;o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.fbx(filepath=str(root/'Gun_Editable.trimmed.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=False,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
print('TRIM_COMPLETE',len(mesh.vertices),len(mesh.polygons))

