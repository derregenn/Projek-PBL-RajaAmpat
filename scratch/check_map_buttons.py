import re

with open('Assets/Prefabs_Reg/MapMenuPanel.prefab', 'r', encoding='utf-8') as f:
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
    if 'PindahKePulau' in doc:
        go_m = re.search(r'm_GameObject:\s*{fileID:\s*(\d+)}', doc)
        arg_m = re.search(r'm_StringArgument:\s*([^\r\n]+)', doc)
        goid = go_m.group(1) if go_m else 'unknown'
        arg = arg_m.group(1) if arg_m else 'unknown'
        name = go_names.get(goid, goid)
        print(f"GO: {name} -> PindahKePulau('{arg}')")

