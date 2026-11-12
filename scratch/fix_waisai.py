import re

def fix_waisai_npcs(scene_path):
    with open(scene_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Fix DialogueCanvas on Warlok (467841095), Bule (1422329284), Bule2 (1488575052)
    canvases = ['467841095', '1422329284', '1488575052']
    for cid in canvases:
        m = re.search(r'(--- !u!224 &' + cid + r'[\s\S]*?m_LocalScale:\s*\{)[^\}]+(\}[\s\S]*?m_AnchoredPosition:\s*\{)[^\}]+(\})', content)
        if m:
            content = content[:m.start()] + m.group(1) + 'x: 0.02726667, y: 0.02726667, z: 0' + m.group(2) + 'x: 0, y: 5.9' + m.group(3) + content[m.end():]
            print(f'Updated DialogueCanvas {cid}')

    # 2. Fix DialogueBox on Warlok (377023892), Bule (1784821097), Bule2 (1240042227)
    boxes = ['377023892', '1784821097', '1240042227']
    for bid in boxes:
        m = re.search(r'(--- !u!224 &' + bid + r'[\s\S]*?m_LocalScale:\s*\{)[^\}]+(\}[\s\S]*?m_AnchoredPosition:\s*\{)[^\}]+(\}[\s\S]*?m_SizeDelta:\s*\{)[^\}]+(\})', content)
        if m:
            content = content[:m.start()] + m.group(1) + 'x: 1, y: 1, z: 1' + m.group(2) + 'x: -0.0013113, y: -47.4' + m.group(3) + 'x: 300, y: 102.69' + m.group(4) + content[m.end():]
            print(f'Updated DialogueBox {bid}')

    # 3. Fix InteractPrompt on Warlok (412491675)
    prompts = ['412491675']
    for pid in prompts:
        m = re.search(r'(--- !u!224 &' + pid + r'[\s\S]*?m_LocalScale:\s*\{)[^\}]+(\}[\s\S]*?m_AnchoredPosition:\s*\{)[^\}]+(\}[\s\S]*?m_SizeDelta:\s*\{)[^\}]+(\})', content)
        if m:
            content = content[:m.start()] + m.group(1) + 'x: 1, y: 1, z: 0' + m.group(2) + 'x: 0, y: -73.5' + m.group(3) + 'x: 200, y: 50' + m.group(4) + content[m.end():]
            print(f'Updated InteractPrompt {pid}')

    with open(scene_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Done fixing 02_Waisai.unity")

if __name__ == '__main__':
    fix_waisai_npcs('Assets/Scenes/02_Waisai.unity')

