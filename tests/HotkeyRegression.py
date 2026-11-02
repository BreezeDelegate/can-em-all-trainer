from pathlib import Path
import re
src=(Path(__file__).resolve().parent.parent/'src'/'TrainerMod.cs').read_text(encoding='utf8')
assert 'GetAsyncKeyState(menuKey)' in src
assert 'GetAsyncKeyState(0x77)' in src
assert 'if ((pressed && !prevKey) || (pressedF8 && !prevF8)) Toggle();' in src
assert src.count('prevKey = pressed;')==1 and src.count('prevF8 = pressedF8;')==1
def press(k,prevk,f8,prevf8):
    return (k and not prevk) or (f8 and not prevf8)
cases=[
('idle',False,False,False,False,False),
('original key down',True,False,False,False,True),
('original held',True,True,False,False,False),
('F8 down',False,False,True,False,True),
('F8 held',False,False,True,True,False),
('both together',True,False,True,False,True),
('F8 pressed while original held',True,True,True,False,True),
('original pressed while F8 held',True,False,True,True,True),
('release',False,True,False,True,False),
('new press after release',False,False,True,False,True),
]
for name,k,prevk,f8,prevf8,expected in cases:
    assert press(k,prevk,f8,prevf8)==expected,name
    print('PASS '+name)
print('HOTKEY_REGRESSIONS='+str(len(cases)))
