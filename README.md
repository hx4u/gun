# Gun by @kil_l_y
## GUN is a command-line process management tool designed to enumerate, control, and terminate system processes efficiently. It supports batch and sequential modes, offers process filtering by name, and includes safeguards against terminating critical system processes. Optional logging and user-configurable flags enable tailored operation for diverse administrative or automation tasks. 
![Gravitational_Beam_Emitter_charging](https://github.com/user-attachments/assets/6a9e0318-9dfe-451f-9d71-5121ce6e7ebb) 
```
Help Menu:
  Enter PID to add it to the queue.
  Enter 'k' to kill all processes in the queue.
  Enter 'exit' to close the app.
  Arguments:
        '--sequence' mode kills the process immediately upon entry.
        '--name' mode lets you kill by process name, .exe, or window title.
        '--gbe' mode kills all possible processes upon 'blame' entry.
        '--safeguard off' disables critical process protection. Use with caution.
        '--gunlog' mode saves a log to gunlog.txt.
  Quick Usage:
        gun -n 'proces name' will kill a process by name without needing to load.
        gun -p 'PID' will kill a process by it's PID.
        gun -l (--list) lists all running processes and quits.
  Typing help at any time shows this menu.
```
