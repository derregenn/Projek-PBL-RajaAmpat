import re

def print_modifications(scene_path):
    print("="*50)
    print("SCENE:", scene_path)
    with open(scene_path, 'r', encoding='utf-8') as f:
        text = f.read()
    
    docs = text.split('--- !u!')
    for doc in docs:
        if doc.startswith('1001 &'):
            header = doc.split('\n')[0].strip()
            # check if it modifies CinemachineCamera
            mods = re.findall(r'-\s*target:.*?propertyPath:\s*([^\r\n]+)\s*value:\s*([^\r\n]*)(?:\s*objectReference:\s*{fileID:\s*(\d+))?', doc, re.DOTALL)
            cam_mods = []
            for prop, val, ref in mods:
                if any(k in prop for k in ['m_Name', 'Lens', 'Target', 'FollowOffset', 'Priority']):
                    cam_mods.append((prop, val, ref))
            if cam_mods:
                print(f"PrefabInstance {header}:")
                for prop, val, ref in cam_mods:
                    print(f"   {prop} = '{val}' (ref: {ref})")

for s in ['Assets/Scenes/04_Misool.unity', 'Assets/Scenes/06_Sawinggrai.unity', 'Assets/Scenes/08_Laut Misool 1.unity', 'Assets/Scenes/08_Laut Misool 2.unity', 'Assets/Scenes/09_Laut Sawinggrai.unity']:
    print_modifications(s)

