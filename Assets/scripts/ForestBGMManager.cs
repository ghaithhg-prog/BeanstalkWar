using UnityEngine;

/// <summary>
/// ForestBGMManager:
/// الموسيقى التصويرية الأسطورية الجديدة لشجرة الفاصولياء العملاقة (BeanstalkWar).
/// تؤلف وتولد مقطوعة أوركسترالية حماسية خيالية تجمع بين:
/// 1. طبول الحرب الإيقاعية الحماسية (Epic War Drums & Taiko).
/// 2. وتريات السيلو والكمان الدافئة الملحمية (Orchestral Celli & Violins).
/// 3. فلوت وناي الطبيعة الأسطوري الخيالي (Celtic Forest Flute).
/// 4. قيثارة الهارب الساحرة المتلألئة (Shimmering Magical Harp).
/// 
/// المقطوعة تعمل بنظام الستيريو (Stereo) وبدوران حلقي لا نهائي (Seamless Loop) بدون أي تقطيع،
/// وتتكامل مع شريط الصوت في قائمة الإيقاف المؤقت (In-Game Pause Menu).
/// </summary>
public class ForestBGMManager : MonoBehaviour
{
    public static ForestBGMManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.38f;

    private AudioSource audioSource;
    private static AudioClip cachedThemeClip;
    private bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject bgmObj = new GameObject("ForestBGMManager");
            bgmObj.AddComponent<ForestBGMManager>();
            DontDestroyOnLoad(bgmObj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D Stereo Background Music
        audioSource.volume = musicVolume;
    }

    void Start()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (cachedThemeClip == null)
        {
            cachedThemeClip = GenerateEpicFantasyTheme();
        }

        if (cachedThemeClip != null)
        {
            audioSource.clip = cachedThemeClip;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (audioSource != null)
        {
            float targetVol = musicVolume;
            if (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameStateManager.GameState.Ended)
            {
                // خفض الموسيقى برقة في شاشة إحصائيات النهاية
                targetVol = musicVolume * 0.5f;
            }

            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVol, 3f * Time.deltaTime);
        }
    }

    public void SetVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (audioSource != null) audioSource.volume = musicVolume;
    }

    /// <summary>
    /// توليد المقطوعة الملحمية: 16 مازورة موسيقية على مقام D Dorian وبسرعة 112 BPM
    /// </summary>
    private static AudioClip GenerateEpicFantasyTheme()
    {
        int sampleRate = 44100;
        int bpm = 112;
        int totalBars = 16;
        float beatsPerBar = 4f;
        float secondsPerBeat = 60f / bpm;
        float totalDuration = totalBars * beatsPerBar * secondsPerBeat; // ~34.2857 seconds
        int totalSamples = (int)(sampleRate * totalDuration);

        float[] leftChannel = new float[totalSamples];
        float[] rightChannel = new float[totalSamples];

        var rng = new System.Random(777);

        // جدول الترددات لمقام D Minor / Dorian (بالهرتز)
        // D3, E3, F3, G3, A3, Bb3, C4, D4, E4, F4, G4, A4, C5, D5
        float D2 = 73.42f, F2 = 87.31f, G2 = 98.00f, A2 = 110.00f, Bb2 = 116.54f, C3 = 130.81f;
        float D3 = 146.83f, E3 = 164.81f, F3 = 174.61f, G3 = 196.00f, A3 = 220.00f, Bb3 = 233.08f, C4 = 261.63f;
        float D4 = 293.66f, E4 = 329.63f, F4 = 349.23f, G4 = 392.00f, A4 = 440.00f, Bb4 = 466.16f, C5 = 523.25f, D5 = 587.33f;

        // تتابع الكوردات عبر الـ 16 مازورة:
        // 0-3:   Dm  (D - F - A)
        // 4-7:   Bb  (Bb - D - F) -> C (C - E - G)
        // 8-11:  Dm  (D - F - A) -> Gm (G - Bb - D)
        // 12-15: Bb  (Bb - D - F) -> C -> A7 (A - C# - E - G) -> Dm
        float[] rootTones = new float[]
        {
            D2, D2, D2, D2,     // Bars 0-3: Dm
            Bb2, Bb2, C3, C3,   // Bars 4-7: Bb -> C
            D2, D2, G2, G2,     // Bars 8-11: Dm -> Gm
            Bb2, Bb2, C3, A2    // Bars 12-15: Bb -> C -> A7 -> resolving smoothly to D2!
        };

        float[][] chordTriads = new float[][]
        {
            new float[] { D3, F3, A3, D4 },
            new float[] { D3, F3, A3, D4 },
            new float[] { D3, F3, A3, D4 },
            new float[] { D3, F3, A3, D4 },

            new float[] { Bb2, D3, F3, Bb3 },
            new float[] { Bb2, D3, F3, Bb3 },
            new float[] { C3, E3, G3, C4 },
            new float[] { C3, E3, G3, C4 },

            new float[] { D3, F3, A3, D4 },
            new float[] { D3, F3, A3, D4 },
            new float[] { G2, Bb2, D3, G3 },
            new float[] { G2, Bb2, D3, G3 },

            new float[] { Bb2, D3, F3, Bb3 },
            new float[] { Bb2, D3, F3, Bb3 },
            new float[] { C3, E3, G3, C4 },
            new float[] { A2, E3, G3, A3 }
        };

        // لحن الفلوت الأسطوري (Celtic Pan Flute Motifs) عبر الموازير:
        // كل مازورة تحمل 4 نغمات أساسية
        float[][] fluteMelody = new float[][]
        {
            new float[] { D4, F4, A4, G4 },
            new float[] { F4, E4, D4, C4 },
            new float[] { D4, F4, G4, A4 },
            new float[] { C5, A4, G4, F4 },

            new float[] { D4, F4, Bb3, D4 },
            new float[] { F4, G4, A4, Bb4 },
            new float[] { C5, G4, E4, G4 },
            new float[] { A4, G4, F4, E4 },

            new float[] { D4, A4, D5, C5 },
            new float[] { A4, G4, F4, G4 },
            new float[] { Bb4, D5, C5, Bb4 },
            new float[] { A4, G4, F4, E4 },

            new float[] { F4, G4, A4, D5 },
            new float[] { D5, C5, Bb4, A4 },
            new float[] { G4, A4, C5, E4 },
            new float[] { F4, E4, C4, D4 } // ينتهي على D4 ليعود بلطف إلى بداية المقطوعة D4
        };

        float barDuration = beatsPerBar * secondsPerBeat;
        float beatDuration = secondsPerBeat;

        // ==========================================
        // 1. توليد طبول الحرب (War Drums & Percussion)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStartTime = bar * barDuration;

            for (int beat = 0; beat < 4; beat++)
            {
                float beatTime = barStartTime + beat * beatDuration;

                // ضربة طبل أساسية (Deep Bass Drum) على النبضة 1 و 3
                if (beat == 0 || beat == 2)
                {
                    AddWarDrumHit(leftChannel, rightChannel, beatTime, sampleRate, 1.0f, 68f, 0.42f);
                }

                // ضربة طبل متوسطة (Tom / War Snare) على النبضة 2 و 4
                if (beat == 1 || beat == 3)
                {
                    AddWarDrumHit(leftChannel, rightChannel, beatTime, sampleRate, 0.72f, 125f, 0.28f);
                }

                // ارتداد إيقاعي حماسي متقطع (Syncopated 8th / 16th hits)
                if (beat == 1 || beat == 3)
                {
                    float syncTime = beatTime + beatDuration * 0.5f;
                    AddWarDrumHit(leftChannel, rightChannel, syncTime, sampleRate, 0.48f, 150f, 0.18f);
                }

                // في الموازير الفردية (Climax Fill) نضيف ضربة سريعة قبيل نهاية المازورة
                if ((bar % 2 == 1) && beat == 3)
                {
                    float rollTime1 = beatTime + beatDuration * 0.25f;
                    float rollTime2 = beatTime + beatDuration * 0.75f;
                    AddWarDrumHit(leftChannel, rightChannel, rollTime1, sampleRate, 0.35f, 180f, 0.12f);
                    AddWarDrumHit(leftChannel, rightChannel, rollTime2, sampleRate, 0.45f, 210f, 0.14f);
                }
            }
        }

        // ==========================================
        // 2. وتريات السيلو والأوركسترا الدافئة (Strings & Chords)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] triad = chordTriads[bar % chordTriads.Length];
            float bassRoot = rootTones[bar % rootTones.Length];

            // نغمة الباص المنخفضة (Deep Double Bass)
            AddSustainedStringNote(leftChannel, rightChannel, barStart, barDuration, bassRoot, sampleRate, 0.26f, -0.1f);

            // وتريات الهارموني (Lush Mid Strings)
            for (int noteIdx = 0; noteIdx < triad.Length; noteIdx++)
            {
                float pan = (noteIdx % 2 == 0) ? -0.35f : 0.35f;
                AddSustainedStringNote(leftChannel, rightChannel, barStart, barDuration, triad[noteIdx], sampleRate, 0.12f, pan);
            }
        }

        // ==========================================
        // 3. قيثارة الهارب الساحرة (Magical Plucked Harp Arpeggios)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] triad = chordTriads[bar % chordTriads.Length];

            // 8 نقرات هارب سريعة ومتلألئة في كل مازورة (8th note arpeggios)
            for (int h = 0; h < 8; h++)
            {
                float harpTime = barStart + h * (barDuration / 8f);
                float freq = triad[h % triad.Length] * 2f; // نغمة علوية متألقة
                float pan = Mathf.Sin(h * 0.8f) * 0.65f;
                AddPluckedHarpNote(leftChannel, rightChannel, harpTime, freq, sampleRate, 0.15f, pan);
            }
        }

        // ==========================================
        // 4. لحن الفلوت الأسطوري (Celtic Forest Pan Flute)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] melody = fluteMelody[bar % fluteMelody.Length];

            for (int m = 0; m < 4; m++)
            {
                float noteTime = barStart + m * beatDuration;
                float noteFreq = melody[m];
                float noteDuration = beatDuration * 0.88f; // Legato phrasing
                AddFluteNote(leftChannel, rightChannel, noteTime, noteDuration, noteFreq, sampleRate, 0.22f);
            }
        }

        // ==========================================
        // 5. التنعيم الحلقي المتقاطع (Seamless Loop Crossfade)
        // ==========================================
        // تطبيق تلاشٍ متقاطع (Smooth Crossfade) على آخر 0.25 ثانية لدمجها تماماً مع أول 0.25 ثانية
        int fadeSamples = (int)(sampleRate * 0.25f);
        for (int i = 0; i < fadeSamples; i++)
        {
            float t = (float)i / fadeSamples;
            int tailIdx = totalSamples - fadeSamples + i;

            // مزج النهاية مع البداية بسلاسة جيبية تمنع أي طقطقة نهائياً
            leftChannel[i] = leftChannel[i] * t + leftChannel[tailIdx] * (1f - t);
            rightChannel[i] = rightChannel[i] * t + rightChannel[tailIdx] * (1f - t);

            // تصفير ذيل النهاية لمنع التكرار
            leftChannel[tailIdx] = leftChannel[tailIdx] * (1f - t);
            rightChannel[tailIdx] = rightChannel[tailIdx] * (1f - t);
        }

        // إنشاء الـ AudioClip الستيريو وضبط البيانات
        float[] interleaved = new float[totalSamples * 2];
        for (int i = 0; i < totalSamples; i++)
        {
            interleaved[i * 2] = Mathf.Clamp(leftChannel[i] * 0.82f, -1f, 1f);
            interleaved[i * 2 + 1] = Mathf.Clamp(rightChannel[i] * 0.82f, -1f, 1f);
        }

        AudioClip themeClip = AudioClip.Create("BeanstalkWar_EpicFantasyBGM", totalSamples, 2, sampleRate, false);
        themeClip.SetData(interleaved, 0);
        return themeClip;
    }

    private static void AddWarDrumHit(float[] left, float[] right, float startTime, int sampleRate, float volume, float basePitch, float length)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(length * sampleRate);
        float phase = 0f;

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;

            // غلاف الضرب والانحدار السريع للطبل
            float env = Mathf.Exp(-t * 12f);
            float punch = Mathf.Exp(-t * 40f) * 1.5f;

            // انحدار التردد لمحاكاة ضربة جلد الطبل الحقيقية
            float currentPitch = basePitch + Mathf.Exp(-t * 28f) * (basePitch * 1.8f);
            phase += 2f * Mathf.PI * currentPitch / sampleRate;

            float drumWave = Mathf.Sin(phase) + Mathf.Sin(phase * 0.5f) * 0.35f;
            float sample = (drumWave + punch) * env * volume * 0.32f;

            left[idx] += sample;
            right[idx] += sample;
        }
    }

    private static void AddSustainedStringNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        float phase1 = 0f;
        float phase2 = 0.5f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / duration;

            // غلاف ناعم (Attack / Sustain / Release)
            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.22f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.22f));
            float env = attack * release;

            // اهتزاز حيوي لطيف (Vibrato)
            float vibrato = Mathf.Sin(2f * Mathf.PI * 4.8f * ((float)s / sampleRate)) * 0.015f;
            float noteFreq = freq * (1f + vibrato);

            phase1 += 2f * Mathf.PI * noteFreq / sampleRate;
            phase2 += 2f * Mathf.PI * (noteFreq * 1.004f) / sampleRate; // جوقة خفيفة (Chorus detune)

            // مزيج نغمي ناعم يشبه وتريات السيلو
            float wave = (Mathf.Sin(phase1) * 0.65f + Mathf.Sin(phase2) * 0.35f);
            float sample = wave * env * volume;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddPluckedHarpNote(float[] left, float[] right, float startTime, float freq, int sampleRate, float volume, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(0.55f * sampleRate);

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        float phase = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;
            float env = Mathf.Exp(-t * 9f);

            phase += 2f * Mathf.PI * freq / sampleRate;
            // نغمة قيثارة مع انسجام علوي متألق (Harmonic Shimmer)
            float wave = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3f) * 0.05f;
            float sample = wave * env * volume;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    private static void AddFluteNote(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);

        float phase = 0f;
        float breathPhase = 0f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            // هجوم تدريجي للنفس ثم استقرار ثم تلاشٍ لطيف
            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.09f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.09f));
            float env = attack * release;

            float vibrato = (s > sampleRate * 0.15f) ? Mathf.Sin(2f * Mathf.PI * 5.2f * ((float)s / sampleRate)) * 0.018f : 0f;
            float noteFreq = freq * (1f + vibrato);

            phase += 2f * Mathf.PI * noteFreq / sampleRate;
            breathPhase += 2f * Mathf.PI * (noteFreq * 2.01f) / sampleRate;

            // صوت الفلوت الخشبي النقي مع لمسة هواء ساحرة
            float flute = Mathf.Sin(phase) * 0.82f + Mathf.Sin(phase * 2f) * 0.15f + Mathf.Sin(breathPhase) * 0.03f;
            float sample = flute * env * volume;

            // في المنتصف مع اتساع ستيريو لطيف
            left[idx] += sample * 0.52f;
            right[idx] += sample * 0.48f;
        }
    }
}
