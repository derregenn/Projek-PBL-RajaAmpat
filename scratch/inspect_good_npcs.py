import re

def inspect_good_npcs():
    scenes = [
        ('Assets/Scenes/02_Waisai.unity', ['TourGuide']),
        ('Assets/Scenes/05_Arborek.unity', ['TourGuide', 'NPC', 'Tetua', 'Warlok']),
        ('Assets/Scenes/10_Gag.unity', ['Tetua_Lemas2', 'Tetua_Lemas3', 'TourGuide'])
    ]
    
    for scene_path, targets in scenes:
        print(f"\n=================== {scene_path} ===================")
        with open(scene_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # Build map of GameObjects
        go_data = {}
        for m in re.finditer(r'--- !u!1 &(\d+)[\s\S]*?m_Component:([\s\S]*?)m_Layer: \d+[\s\S]*?m_Name:\s*([^\r\n]+)', content):
            goid, comps, name = m.group(1), m.group(2), m.group(3)
            comp_ids = re.findall(r'fileID:\s*(\d+)', comps)
            go_data[goid] = {'name': name, 'components': comp_ids}

        # Build transform map
        tr_data = {}
        for m in re.finditer(r'--- !u!(?:4|224) &(\d+)[\s\S]*?m_GameObject:\s*\{fileID:\s*(\d+)\}[\s\S]*?m_LocalScale:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^\}]+)\}[\s\S]*?m_LocalPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^\}]+)\}', content):
            tr_id, go_id, sx, sy, sz, px, py, pz = m.group(1), m.group(2), m.group(3), m.group(4), m.group(5), m.group(6), m.group(7), m.group(8)
            block = m.group(0)
            anchored = re.search(r'm_AnchoredPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+)\}', block)
            size = re.search(r'm_SizeDelta:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+)\}', block)
            father = re.search(r'm_Father:\s*\{fileID:\s*(\d+)\}', block)
            
            tr_data[tr_id] = {
                'go_id': go_id,
                'scale': (float(sx), float(sy), float(sz)),
                'pos': (float(px), float(py), float(pz)),
                'anchored': (float(anchored.group(1)), float(anchored.group(2))) if anchored else None,
                'size': (float(size.group(1)), float(size.group(2))) if size else None,
                'father': father.group(1) if father else None,
                'type': 'RectTransform' if 'm_AnchoredPosition' in block else 'Transform'
            }

        # Match targets
        for go_id, d in go_data.items():
            if any(t.lower() in d['name'].lower() for t in targets):
                # find this GO's transform
                my_tid = None
                my_tr = None
                for tid, tr in tr_data.items():
                    if tr['go_id'] == go_id:
                        my_tid = tid
                        my_tr = tr
                        break
                if my_tr:
                    print(f"\nGO: {d['name']} (ID {go_id})")
                    print(f"  Transform: pos={my_tr['pos']}, scale={my_tr['scale']}")
                    
                    # Find direct children
                    children = [ (tid, tr) for tid, tr in tr_data.items() if tr.get('father') == my_tid ]
                    for c_tid, ctr in children:
                        c_go = go_data.get(ctr['go_id'], {})
                        c_name = c_go.get('name', 'Unknown')
                        print(f"    Child: {c_name} (TR {c_tid}) | Type={ctr['type']} | Pos={ctr['pos']} | Anchored={ctr['anchored']} | Scale={ctr['scale']} | Size={ctr['size']}")
                        # grand children
                        grandchildren = [ (gtid, gtr) for gtid, gtr in tr_data.items() if gtr.get('father') == c_tid ]
                        for g_tid, gtr in grandchildren:
                            g_go = go_data.get(gtr['go_id'], {})
                            g_name = g_go.get('name', 'Unknown')
                            print(f"      GrandChild: {g_name} (TR {g_tid}) | Type={gtr['type']} | Pos={gtr['pos']} | Anchored={gtr['anchored']} | Scale={gtr['scale']} | Size={gtr['size']}")

inspect_good_npcs()
