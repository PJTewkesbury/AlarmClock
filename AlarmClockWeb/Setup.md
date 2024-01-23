# AlarmClock Setup

## For Text To Speech

https://github.com/Shadowsith/qpicospeaker

```
sudo apt-get install sox mplayer libttspico-utils

pico2wave -w speech.wav -l en-GB "Text To Speak"
play speech.wav
```

## Permissions
/AlarmClock/Script files need execute permissions for PI user

/opt needs rwx permissions for PI user

