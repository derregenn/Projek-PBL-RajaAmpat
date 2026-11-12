import re

def inspect_cams(scene_path):
    print("="*40)
    print("SCENE:", scene_path)
    with open(scene_path, 'r', encoding='utf-8') as f:
        text = f.read()
    
    docs = text.split('--- !u!')
    for doc in docs:
        if any(k in doc for k in ['CinemachineCamera', 'ObjectiveCamera', 'CameraPanner', 'Cam_View', 'CamView']):
            # print doc type and id
            lines = doc.split('\n')
            header = lines[0].strip()
            print(header)
            for l in lines:
                if any(k in l for k in ['m_Name', 'Lens', 'OrthographicSize', 'TrackingTarget', 'FollowOffset', 'm_Priority', 'propertyPath: m_Name', 'propertyPath: Lens', 'propertyPath: Target']):
                    print("  ", l.strip())

inspect_cams('Assets/Scenes/04_Misool.unity')
inspect_cams('Assets/Scenes/06_Sawinggrai.unity')
inspect_cams('Assets/Scenes/08_Laut Misool 1.unity')
inspect_cams('Assets/Scenes/08_Laut Misool 2.unity')
inspect_cams('Assets/Scenes/09_Laut Sawinggrai.unity')

