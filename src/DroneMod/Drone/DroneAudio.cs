using UnityEngine;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// Motor noise, synthesised at runtime so the mod does not have to ship audio
    /// assets. Two sources: a spatialised one on the airframe (what you hear from
    /// the cockpit as the drone goes past) and a flat one that represents the
    /// goggles' own audio, which stays audible at any range.
    /// </summary>
    internal sealed class DroneAudio : MonoBehaviour
    {
        private AudioSource _world;
        private AudioClip _clip;

        public void Build()
        {
            _clip = SynthesiseMotorLoop();

            _world = gameObject.AddComponent<AudioSource>();
            _world.clip = _clip;
            _world.loop = true;
            _world.playOnAwake = false;
            _world.spatialBlend = 1f;
            _world.rolloffMode = AudioRolloffMode.Logarithmic;
            _world.minDistance = 3f;
            _world.maxDistance = 300f;
            _world.volume = 0f;
            _world.pitch = 1f;

            try
            {
                _world.Play();
            }
            catch (System.Exception e)
            {
                ModLog.Trace("Could not start drone audio: " + e.Message);
            }
        }

        /// <summary>
        /// Updates the motor note. <paramref name="load"/> is thrust over hover thrust.
        /// </summary>
        public void UpdateMotors(float load, bool alive)
        {
            if (_world == null)
            {
                return;
            }

            if (!alive)
            {
                _world.volume = Mathf.MoveTowards(_world.volume, 0f, Time.deltaTime * 2f);
                return;
            }

            float normalised = Mathf.Clamp(load, 0f, 2.5f);
            _world.volume = Mathf.Lerp(0.05f, 0.55f, Mathf.Clamp01(normalised / 1.8f));
            _world.pitch = Mathf.Lerp(0.72f, 1.55f, Mathf.Clamp01(normalised / 2.0f));
        }

        /// <summary>
        /// A one-second loop built from the blade-passing fundamental and its
        /// harmonics, plus a little broadband hiss for the airflow.
        /// </summary>
        private static AudioClip SynthesiseMotorLoop()
        {
            const int sampleRate = 44100;
            const int samples = sampleRate;
            const float fundamental = 165f;

            float[] data = new float[samples];
            float previousNoise = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                float value = 0f;
                value += Mathf.Sin(2f * Mathf.PI * fundamental * t) * 0.42f;
                value += Mathf.Sin(2f * Mathf.PI * fundamental * 2f * t) * 0.26f;
                value += Mathf.Sin(2f * Mathf.PI * fundamental * 3f * t) * 0.14f;
                value += Mathf.Sin(2f * Mathf.PI * fundamental * 4.5f * t) * 0.07f;

                // Slight detune between the four motors gives the characteristic beat.
                value += Mathf.Sin(2f * Mathf.PI * (fundamental * 1.02f) * t) * 0.18f;
                value += Mathf.Sin(2f * Mathf.PI * (fundamental * 0.97f) * t) * 0.18f;

                // Low-passed white noise for prop wash.
                float noise = Random.value * 2f - 1f;
                previousNoise = Mathf.Lerp(previousNoise, noise, 0.25f);
                value += previousNoise * 0.16f;

                data[i] = Mathf.Clamp(value * 0.55f, -1f, 1f);
            }

            // Cross-fade the tail into the head so the loop point is inaudible.
            int fade = sampleRate / 100;
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                data[i] = Mathf.Lerp(data[samples - fade + i], data[i], t);
            }

            AudioClip clip = AudioClip.Create("DroneMotors", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
