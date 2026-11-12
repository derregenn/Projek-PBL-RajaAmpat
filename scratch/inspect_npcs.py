import re

def inspect_scene_npcs(scene_path):
    print("=== Inspecting:", scene_path)
    current_gameobject = None
    go_names = {} # fileID -> name
    transforms = {} # fileID -> {go_id, scale, pos}
    rect_transforms = {} # fileID -> {go_id, scale, pos, size, parent}
    
    with open(scene_path, 'r', encoding='utf-8') as f:
        current_type = None
        current_id = None
        current_data = {}
        for line in f:
            if line.startswith('--- !u!'):
                # save previous
                if current_type == '1' and current_id:
                    go_names[current_id] = current_data.get('m_Name', '')
                elif current_type == '4' and current_id:
                    transforms[current_id] = current_data
                elif current_type == '224' and current_id:
                    rect_transforms[current_id] = current_data

                parts = line.split('&')
                if len(parts) > 1:
                    current_id = parts[1].strip()
                    current_type = line.split('!u!')[1].split(' ')[0]
                    current_data = {}
                continue

            if current_type == '1': # GameObject
                if 'm_Name:' in line:
                    current_data['m_Name'] = line.split('m_Name:')[1].strip()
            elif current_type in ('4', '224'): # Transform or RectTransform
                if 'm_GameObject:' in line:
                    m = re.search(r'fileID:\s*(\d+)', line)
                    if m: current_data['go_id'] = m.group(1)
                elif 'm_Father:' in line:
                    m = re.search(r'fileID:\s*(\d+)', line)
                    if m: current_data['father_id'] = m.group(1)
                elif 'm_LocalScale:' in line:
                    current_data['scale'] = line.strip()
                elif 'm_LocalPosition:' in line:
                    current_data['pos'] = line.strip()
                elif 'm_AnchoredPosition:' in line:
                    current_data['anchored_pos'] = line.strip()
                elif 'm_SizeDelta:' in line:
                    current_data['size'] = line.strip()

    # Now inspect NPCs
    for tr_id, tr in transforms.items():
        go_name = go_names.get(tr.get('go_id'), '')
        if any(k in go_name for k in ['TourGuide', 'Warlok', 'PetugasHub', 'Bule', 'Tetua', 'Peneliti', 'NPC']):
            print(f"NPC: {go_name} (TR {tr_id})")
            print(f"  Scale: {tr.get('scale')}, Pos: {tr.get('pos')}")

    for rtr_id, rtr in rect_transforms.items():
        go_name = go_names.get(rtr.get('go_id'), '')
        if 'DialogueCanvas' in go_name or 'InteractPrompt' in go_name or 'DialogueBox' in go_name:
            print(f"UI: {go_name} (RTR {rtr_id})")
            print(f"  Scale: {rtr.get('scale')}, Pos: {rtr.get('pos')}, AnchoredPos: {rtr.get('anchored_pos')}, Size: {rtr.get('size')}")

if __name__ == '__main__':
    inspect_scene_npcs('Assets/Scenes/02_Waisai.unity')

