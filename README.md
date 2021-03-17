# LayOff
Your local HR Department for Windows 10 keyboard layouts. Removes specified keyboard layouts from your system.

## Command Line Syntax
```
C:\Users\Yuubari> layoff /?
Unloads keyboard layouts.

Syntax:

layoff /?
  Shows this help message.

layoff /L
  Lists layouts currently present.

layoff ID …
  Unloads listed layout IDs. Layout IDs must be hexadecimal numbers
  prefixed with 0x. Four-digit IDs are treated as language IDs, and
  all layouts related to that language ID are unloaded.

layoff
  Unloads layouts associated with US English and UK English; equals
  to "layoff 0x0809 0x0409".
```
