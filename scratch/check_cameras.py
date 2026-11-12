import re, glob, os

scenes = [
    'Assets/Scenes/04_Misool.unity',
    'Assets/Scenes/06_Sawinggrai.unity',
    'Assets/Scenes/07_Friwen.unity',
    'Assets/Scenes/08_Laut Misool 1.unity',
    'Assets/Scenes/08_Laut Misool 2.unity',
    'Assets/Scenes/09_Laut Sawinggrai.unity',
    'Assets/Scenes/10_Gag.unity'
]

for s in scenes:
    print('==============================')
    print('SCENE:', s)
    with open(s, 'r', encoding='utf-8') as f:
        text = f.read()
    
    docs = text.split('--- !u!')
    go_names = {}
    for doc in docs:
        lines = doc.split('\n')
        if len(lines) > 0 and '&' in lines[0] and lines[0].split('&')[0].strip() == '1':
            m = re.search(r'm_Name:\s*([^\r\n]+)', doc)
            if m:
                go_names[lines[0].split('&')[1].strip()] = m.group(1).strip()

    for doc in docs:
        # CinemachineCamera or CameraPanner
        if 'guid: f9dfa5b682dcd46bda6128250e975f58' in doc or 'CinemachineCamera' in doc or 'CameraPanner' in doc:
            lines = doc.split('\n')
            header = lines[0].strip()
            goid_m = re.search(r'm_GameObject:\s*{fileID:\s*(\d+)}', doc)
            goid = goid_m.group(1) if goid_m else 'unknown'
            name = go_names.get(goid, 'unknown')
            
            target_m = re.search(r'TrackingTarget:\s*{fileID:\s*(\d+)}', doc)
            target_id = target_m.group(1) if target_m else 'none'
            target_name = 'none'
            if target_id != 'none' and target_id != '0':
                # find target go
                for d in docs:
                    if f'&{target_id}' in d.split('\n')[0]:
                        tg_m = re.search(r'm_GameObject:\s*{fileID:\s*(\d+)}', d)
                        tg_goid = tg_m.group(1) if tg_m else target_id
                        target_name = go_names.get(tg_goid, tg_goid)
                        break

            lens_m = re.search(r'OrthographicSize:\s*([^\r\n]+)', doc)
            lens = lens_m.group(1) if lens_m else 'unknown'
            prio_m = re.search(r'm_Value:\s*(\d+)', doc)
            prio = prio_m.group(1) if prio_m else 'unknown'
            print(f"Cam: '{name}' | Lens: {lens} | Priority: {prio} | Target: '{target_name}' ({target_id})")

