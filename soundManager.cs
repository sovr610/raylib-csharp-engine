using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

public class AudioManager
{
    private Dictionary<string, Sound> sounds;
    private Dictionary<string, Vector3> soundPositions;
    private Dictionary<string, float> soundVolumes;
    private Dictionary<string, float> soundPitches;
    private Dictionary<string, bool> isPlaying;

    public AudioManager()
    {
        Raylib.InitAudioDevice();
        sounds = new Dictionary<string, Sound>();
        soundPositions = new Dictionary<string, Vector3>();
        soundVolumes = new Dictionary<string, float>();
        soundPitches = new Dictionary<string, float>();
        isPlaying = new Dictionary<string, bool>();
    }

    public void AddSound(string id, string filename, bool is3D = false)
    {
        if (!sounds.ContainsKey(id))
        {
            Sound sound = Raylib.LoadSound(filename);
            sounds[id] = sound;
            soundVolumes[id] = 1.0f;
            soundPitches[id] = 1.0f;
            isPlaying[id] = false;
            if (is3D)
            {
                soundPositions[id] = Vector3.Zero;
            }
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' already exists.");
        }
    }

    public void DeleteSound(string id)
    {
        if (sounds.ContainsKey(id))
        {
            Raylib.UnloadSound(sounds[id]);
            sounds.Remove(id);
            soundVolumes.Remove(id);
            soundPitches.Remove(id);
            soundPositions.Remove(id);
            isPlaying.Remove(id);
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void PlaySound(string id)
    {
        if (sounds.ContainsKey(id))
        {
            Raylib.PlaySound(sounds[id]);
            isPlaying[id] = true;
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void PauseSound(string id)
    {
        if (sounds.ContainsKey(id))
        {
            Raylib.PauseSound(sounds[id]);
            isPlaying[id] = false;
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void ResumeSound(string id)
    {
        if (sounds.ContainsKey(id))
        {
            Raylib.ResumeSound(sounds[id]);
            isPlaying[id] = true;
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void StopSound(string id)
    {
        if (sounds.ContainsKey(id))
        {
            Raylib.StopSound(sounds[id]);
            isPlaying[id] = false;
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void SetSoundVolume(string id, float volume)
    {
        if (sounds.ContainsKey(id))
        {
            soundVolumes[id] = Raylib.Clamp(volume, 0.0f, 1.0f);
            Raylib.SetSoundVolume(sounds[id], soundVolumes[id]);
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void SetSoundPitch(string id, float pitch)
    {
        if (sounds.ContainsKey(id))
        {
            soundPitches[id] = Raylib.Clamp(pitch, 0.01f, 10.0f);
            Raylib.SetSoundPitch(sounds[id], soundPitches[id]);
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
        }
    }

    public void SetSoundPosition(string id, Vector3 position)
    {
        if (soundPositions.ContainsKey(id))
        {
            soundPositions[id] = position;
            UpdateSoundPosition(id);
        }
        else
        {
            Console.WriteLine($"3D Sound with id '{id}' not found.");
        }
    }

    private void UpdateSoundPosition(string id)
    {
        if (soundPositions.ContainsKey(id) && sounds.ContainsKey(id))
        {
            Vector3 position = soundPositions[id];
            Raylib.SetSoundPosition(sounds[id], position);
        }
    }

    public void Update3DSounds(Vector3 listenerPosition)
    {
        foreach (var id in soundPositions.Keys)
        {
            if (sounds.ContainsKey(id) && isPlaying[id])
            {
                Vector3 relativePosition = soundPositions[id] - listenerPosition;
                float distance = relativePosition.Length();
                float volume = 1.0f / (1.0f + distance * 0.1f); // Simple distance-based attenuation
                SetSoundVolume(id, volume * soundVolumes[id]);
                UpdateSoundPosition(id);
            }
        }
    }

    public bool IsSoundPlaying(string id)
    {
        if (sounds.ContainsKey(id))
        {
            return Raylib.IsSoundPlaying(sounds[id]);
        }
        else
        {
            Console.WriteLine($"Sound with id '{id}' not found.");
            return false;
        }
    }

    public void Cleanup()
    {
        foreach (var sound in sounds.Values)
        {
            Raylib.UnloadSound(sound);
        }
        sounds.Clear();
        soundPositions.Clear();
        soundVolumes.Clear();
        soundPitches.Clear();
        isPlaying.Clear();
        Raylib.CloseAudioDevice();
    }
}