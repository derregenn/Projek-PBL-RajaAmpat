import glob
import re
import os

def main():
    scenes = glob.glob('Assets/Scenes/*.unity')

    for scene_path in scenes:
        with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
            text = f.read()

        modified = False

        # 1. Find all GameObjects for DialogueBox, DialogueCanvas, InteractPrompt
        go_matches = re.finditer(r'--- !u!1 &(\d+)\nGameObject:[\s\S]*?m_Name: (DialogueBox|DialogueCanvas|InteractPrompt)[\s\S]*?(?=---|\Z)', text)
        
        rt_updates = {}
        for match in go_matches:
            go_id = match.group(1)
            name = match.group(2)
            go_block = match.group(0)
            
            # find first component which is RectTransform
            m_comp = re.search(r'- component: {fileID: (\d+)}', go_block)
            if m_comp:
                rt_id = m_comp.group(1)
                rt_updates[rt_id] = name

        # 2. Update the RectTransform blocks
        def replace_rt(match):
            nonlocal modified
            header = match.group(1)
            rt_id = match.group(3)
            body = match.group(4)
            
            if rt_id in rt_updates:
                name = rt_updates[rt_id]
                modified = True
                
                if name == 'DialogueBox':
                    body = re.sub(r'm_Pivot: \{x: [^,]+, y: [^\}]+\}', 'm_Pivot: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchorMin: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMin: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchorMax: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMax: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchoredPosition: \{x: [^,]+, y: [^\}]+\}', 'm_AnchoredPosition: {x: 0, y: 0}', body)
                    body = re.sub(r'm_SizeDelta: \{x: [^,]+, y: [^\}]+\}', 'm_SizeDelta: {x: 300, y: 102.69}', body)
                elif name == 'DialogueCanvas':
                    body = re.sub(r'm_Pivot: \{x: [^,]+, y: [^\}]+\}', 'm_Pivot: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchorMin: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMin: {x: 0, y: 0}', body)
                    body = re.sub(r'm_AnchorMax: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMax: {x: 0, y: 0}', body)
                    body = re.sub(r'm_AnchoredPosition: \{x: ([^,]+), y: [^\}]+\}', r'm_AnchoredPosition: {x: \1, y: 4.5}', body)
                    body = re.sub(r'm_SizeDelta: \{x: [^,]+, y: [^\}]+\}', 'm_SizeDelta: {x: 300, y: 102.69}', body)
                elif name == 'InteractPrompt':
                    body = re.sub(r'm_Pivot: \{x: [^,]+, y: [^\}]+\}', 'm_Pivot: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchorMin: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMin: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchorMax: \{x: [^,]+, y: [^\}]+\}', 'm_AnchorMax: {x: 0.5, y: 0}', body)
                    body = re.sub(r'm_AnchoredPosition: \{x: [^,]+, y: [^\}]+\}', 'm_AnchoredPosition: {x: 0, y: 15}', body)
                    body = re.sub(r'm_SizeDelta: \{x: [^,]+, y: [^\}]+\}', 'm_SizeDelta: {x: 200, y: 50}', body)
                    
                return f'{header}--- !u!224 &{rt_id}\nRectTransform:\n{body}'
                
            return match.group(0)

        text = re.sub(r'(^|\n)(--- !u!224 &(\d+)\nRectTransform:\n)([\s\S]*?)(?=\n---|\Z)', replace_rt, text)

        # 3. In PrefabInstance modifications, update m_Pivot.y = 0 for DialogueBox / DialogueCanvas overrides
        def replace_mod(m):
            nonlocal modified
            prop = m.group(1)
            val = m.group(2)
            if prop in ['m_Pivot.y', 'm_AnchorMin.y', 'm_AnchorMax.y'] and val.strip() == '0.5':
                modified = True
                return f'propertyPath: {prop}\n      value: 0'
            return m.group(0)

        text = re.sub(r'propertyPath: (m_Pivot\.y|m_AnchorMin\.y|m_AnchorMax\.y)\n      value: ([^\n]+)', replace_mod, text)

        if modified:
            with open(scene_path, 'w', encoding='utf-8') as f:
                f.write(text)
            print(f'Updated {scene_path}')

if __name__ == '__main__':
    main()
