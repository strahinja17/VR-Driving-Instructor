using UnityEngine;

public class CarAudioController : MonoBehaviour
{

    [Header("Volume Controls")]
    public float engineIdleVolume = 1f;
    public float engineHighVolume = 1f;
    public float windVolume = 1f;
    public float skidVolume = 1f;
    public float blinkerVolume = 1f;


    [Header("Engine Audio Sources")]
    public AudioSource engineIdleSource;  // looping idle hum
    public AudioSource engineHighSource;  // looping high rev sound

    [Header("Engine Clips")]
    public AudioClip engineIdleClip;  
    public AudioClip engineHighClip;

    [Header("Wind Noise")]
    public AudioSource windSource;
    public AudioClip windClip;

    [Header("Tire Skid")]
    public AudioSource skidSource;
    public AudioClip skidClip;

    [Header("Blinker (optional)")]
    public AudioSource blinkerSource;
    public AudioClip blinkerTick;

    [Header("Settings")]
    public float maxSpeed = 300f;
    public float skidThreshold = 0.35f;

    private Rigidbody rb;
    private float throttle01; // 0..1 coming from your input
    private float rpm01;      // 0..1 value coming from your rpm system

    // external controllers call this to update engine sound
    public void SetThrottle(float t) => throttle01 = Mathf.Clamp01(t);
    public void SetRpmNormalized(float r) => rpm01 = Mathf.Clamp01(r);

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // assign clips
        if (engineIdleSource && engineIdleClip)
        {
            engineIdleSource.clip = engineIdleClip;
            engineIdleSource.loop = true;
            engineIdleSource.Play();
        }

        if (engineHighSource && engineHighClip)
        {
            engineHighSource.clip = engineHighClip;
            engineHighSource.loop = true;
            engineHighSource.Play();
        }

        if (windSource && windClip)
        {
            windSource.clip = windClip;
            windSource.loop = true;
            windSource.Play();
        }

        if (skidSource && skidClip)
        {
            skidSource.clip = skidClip;
            skidSource.loop = true;
            skidSource.Play();
            skidSource.volume = 0;
        }
    }

    void Update()
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        // -------- ENGINE MIXING --------
        engineIdleSource.volume = Mathf.Lerp(1f, 0f, rpm01) * engineIdleVolume;
        engineHighSource.volume = Mathf.Lerp(0f, 1f, rpm01) * engineHighVolume;

        // Both sources change pitch for smooth blending
        engineIdleSource.pitch = Mathf.Lerp(0.8f, 1.3f, rpm01);
        engineHighSource.pitch = Mathf.Lerp(0.5f, 2.0f, rpm01);

        // -------- WIND NOISE --------
        float windNorm = Mathf.InverseLerp(20f, maxSpeed, speedKmh);
        windSource.volume = Mathf.Lerp(0f, 0.6f, windNorm) * windVolume;
        windSource.pitch = Mathf.Lerp(0.5f, 1.5f, windNorm);

        // -------- SKID SOUND --------
        // Basic slip detection: steer angle vs velocity direction
        float slipAmount = Vector3.Angle(transform.forward, rb.linearVelocity.normalized) / 90f;

        if (slipAmount > skidThreshold && speedKmh > 10f)
        {
            skidSource.volume = Mathf.Lerp(skidSource.volume, 1f, Time.deltaTime * 5f) * skidVolume;

            skidSource.pitch = Mathf.Lerp(0.8f, 1.4f, slipAmount);
        }
        else
        {
            skidSource.volume = Mathf.Lerp(skidSource.volume, 0f, Time.deltaTime * 3f) * skidVolume;
        }
    }

    // Call this from CarBlinkers when blink toggles ON
    public void PlayBlinkerTick()
    {
        if (blinkerSource && blinkerTick)
        {
            blinkerSource.volume = blinkerVolume;
            blinkerSource.PlayOneShot(blinkerTick);
        }
    }
}
