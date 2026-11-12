import re, os

def inspect_scene(scene_path, target_names):
    if not os.path.exists(scene_path):
        print(f"File not found: {scene_path}")
        return
    with open(scene_path, 'r', encoding='utf-8') as f:
        text = f.read()
    
    docs = text.split('--- !u!')
    go_dict = {}
    tr_dict = {}
    rt_dict = {}
    
    for doc in docs:
        if not doc.strip(): continue
        lines = doc.split('\n')
        header = lines[0].strip()
        parts = header.split('&')
        if len(parts) < 2: continue
        type_id, file_id = parts[0].strip(), parts[1].strip()
        
        if type_id == '1':
            name_m = re.search(r'm_Name:\s*([^\r\n]+)', doc)
            if name_m:
                go_dict[file_id] = {'name': name_m.group(1).strip(), 'doc': doc}
        elif type_id in ['4', '224']:
            go_m = re.search(r'm_GameObject:\s*{fileID:\s*(\d+)}', doc)
            scale_m = re.search(r'm_LocalScale:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}', doc)
            pos_m = re.search(r'm_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}', doc)
            apos_m = re.search(r'm_AnchoredPosition:\s*{x:\s*([^,]+),\s*y:\s*([^}]+)}', doc)
            size_m = re.search(r'm_SizeDelta:\s*{x:\s*([^,]+),\s*y:\s*([^}]+)}', doc)
            father_m = re.search(r'm_Father:\s*{fileID:\s*(\d+)}', doc)
            children_m = re.findall(r'-\s*{fileID:\s*(\d+)}', doc)
            
            info = {
                'file_id': file_id,
                'go_id': go_m.group(1) if go_m else None,
                'scale': (scale_m.group(1), scale_m.group(2), scale_m.group(3)) if scale_m else None,
                'pos': (pos_m.group(1), pos_m.group(2), pos_m.group(3)) if pos_m else None,
                'apos': (apos_m.group(1), apos_m.group(2)) if apos_m else None,
                'size': (size_m.group(1), size_m.group(2)) if size_m else None,
                'father': father_m.group(1) if father_m else None,
                'children': children_m,
                'is_rt': (type_id == '224')
            }
            if type_id == '4': tr_dict[file_id] = info
            else: rt_dict[file_id] = info

    all_tr = {**tr_dict, **rt_dict}
    for fid, tr in all_tr.items():
        go_id = tr['go_id']
        if go_id in go_dict:
            go_name = go_dict[go_id]['name']
            for t_name in target_names:
                if t_name.lower() in go_name.lower():
                    print(f"[{scene_path}] GO: {go_name} (fid {fid}, go {go_id})")
                    print(f"   Scale: {tr['scale']}, Pos: {tr['pos']}, APos: {tr['apos']}, Size: {tr['size']}")
                    for cid in tr['children']:
                        if cid in all_tr:
                            c_tr = all_tr[cid]
                            c_goid = c_tr['go_id']
                            c_name = go_dict[c_goid]['name'] if c_goid in go_dict else 'unknown'
                            print(f"     Child: {c_name} (cid {cid}) Scale: {c_tr['scale']}, APos: {c_tr['apos']}, Size: {c_tr['size']}")
                            for gcid in c_tr['children']:
                                if gcid in all_tr:
                                    gc_tr = all_tr[gcid]
                                    gc_goid = gc_tr['go_id']
                                    gc_name = go_dict[gc_goid]['name'] if gc_goid in go_dict else 'unknown'
                                    print(f"       Grandchild: {gc_name} Scale: {gc_tr['scale']}, APos: {gc_tr['apos']}, Size: {gc_tr['size']}")

if __name__ == '__main__':
    inspect_scene('Assets/Scenes/10_Gag.unity', ['Lemas', 'TourGuide'])
    inspect_scene('Assets/Scenes/05_Arborek.unity', ['TourGuide', 'Tetua', 'Warlok', 'NPC'])

