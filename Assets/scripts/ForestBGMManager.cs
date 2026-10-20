using UnityEngine;

/// <summary>
/// ForestBGMManager:
/// الموسيقى التصويرية الأسطورية لشجرة الفاصولياء العملاقة (BeanstalkWar).
/// تحتوي على طبول حرب سينمائية حماسية (Cinematic Taiko & Battle Drums) بنبضات قوية وواضحة،
/// مع أبواق الحرب الأوركسترالية (Heroic Brass Horns)، ووتريات السيلو، وفلوت الطبيعة الساحر وقيثارة الهارب.
/// </summary>
public class ForestBGMManager : MonoBehaviour
{
    public static ForestBGMManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.45f;

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
    /// توليد المقطوعة الملحمية: 16 مازورة على مقام D Dorian وبسرعة 114 BPM
    /// بطبول حرب قوية وبارزة وإيقاع حماسي متواصل
    /// </summary>
    public static AudioClip GenerateEpicFantasyTheme()
    {
        int sampleRate = 44100;
        int bpm = 114;
        int totalBars = 16;
        float beatsPerBar = 4f;
        float secondsPerBeat = 60f / bpm;
        float totalDuration = totalBars * beatsPerBar * secondsPerBeat;
        int totalSamples = (int)(sampleRate * totalDuration);

        float[] leftChannel = new float[totalSamples];
        float[] rightChannel = new float[totalSamples];

        // ترددات مقام D Minor / Dorian
        float D2 = 73.42f, G2 = 98.00f, A2 = 110.00f, Bb2 = 116.54f, C3 = 130.81f;
        float D3 = 146.83f, E3 = 164.81f, F3 = 174.61f, G3 = 196.00f, A3 = 220.00f, Bb3 = 233.08f, C4 = 261.63f;
        float D4 = 293.66f, E4 = 329.63f, F4 = 349.23f, G4 = 392.00f, A4 = 440.00f, Bb4 = 466.16f, C5 = 523.25f, D5 = 587.33f;

        float[] rootTones = new float[]
        {
            D2, D2, D2, D2,
            Bb2, Bb2, C3, C3,
            D2, D2, G2, G2,
            Bb2, Bb2, C3, A2
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
            new float[] { F4, E4, C4, D4 }
        };

        float barDuration = beatsPerBar * secondsPerBeat;
        float beatDuration = secondsPerBeat;

        // ==========================================
        // 1. طبول الحرب السينمائية القوية (Epic Cinematic Taiko & War Percussion)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStartTime = bar * barDuration;

            for (int beat = 0; beat < 4; beat++)
            {
                float beatTime = barStartTime + beat * beatDuration;

                // ضربة طبل تاييكو مدوية عميقة (Heavy Sub-Bass Taiko) على 1 و 3
                if (beat == 0 || beat == 2)
                {
                    AddHeavyTaiko(leftChannel, rightChannel, beatTime, sampleRate, 1.35f);

                    // ارتداد خفيف حماسي بعد الضربة الأولى
                    if (beat == 0)
                    {
                        AddHeavyTaiko(leftChannel, rightChannel, beatTime + beatDuration * 0.75f, sampleRate, 0.75f);
                    }
                }

                // ضربة طبل الميدان الحادة العسكرية (War Field Snare & Rim Strike) على 2 و 4
                if (beat == 1 || beat == 3)
                {
                    AddWarSnare(leftChannel, rightChannel, beatTime, sampleRate, 0.95f);
                }

                // إيقاع الخيل والفرسان الحماسي (Galloping 16th Rhythm)
                float gallopTime = beatTime + beatDuration * 0.5f;
                AddWarTom(leftChannel, rightChannel, gallopTime, sampleRate, 0.65f, 130f, 0.25f);

                // ارتداد إيقاعي في النصف الثاني من المازورة
                if (beat >= 2)
                {
                    float subBeatTime = beatTime + beatDuration * 0.25f;
                    AddWarTom(leftChannel, rightChannel, subBeatTime, sampleRate, 0.55f, 175f, -0.3f);
                }

                // ضربات التوم المتسارعة ونهايات الموازير (Climax Battle Fills)
                if ((bar % 2 == 1) && beat == 3)
                {
                    AddWarTom(leftChannel, rightChannel, beatTime + beatDuration * 0.25f, sampleRate, 0.7f, 210f, -0.4f);
                    AddWarTom(leftChannel, rightChannel, beatTime + beatDuration * 0.50f, sampleRate, 0.8f, 160f, 0.0f);
                    AddWarTom(leftChannel, rightChannel, beatTime + beatDuration * 0.75f, sampleRate, 0.95f, 110f, 0.4f);
                }
            }
        }

        // ==========================================
        // 2. أبواق الحرب البطولية الملحمية (Heroic French Horns & Brass Fanfare)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float root = rootTones[bar % rootTones.Length];

            // في المقاطع الحماسية (Bars 4-7 و 12-15) تدوي الأبواق الملكية الملحمية
            if ((bar >= 4 && bar <= 7) || (bar >= 12 && bar <= 15))
            {
                AddHeroicHorn(leftChannel, rightChannel, barStart, barDuration * 0.92f, root * 2f, sampleRate, 0.28f);
                AddHeroicHorn(leftChannel, rightChannel, barStart + beatDuration * 2f, barDuration * 0.45f, root * 3f, sampleRate, 0.22f);
            }
        }

        // ==========================================
        // 3. وتريات السيلو والأوركسترا الدافئة (Strings & Chords)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] triad = chordTriads[bar % chordTriads.Length];
            float bassRoot = rootTones[bar % rootTones.Length];

            // باص السيلو العميق
            AddSustainedStringNote(leftChannel, rightChannel, barStart, barDuration, bassRoot, sampleRate, 0.28f, -0.1f);

            // وتريات الهارموني الوسطى
            for (int noteIdx = 0; noteIdx < triad.Length; noteIdx++)
            {
                float pan = (noteIdx % 2 == 0) ? -0.32f : 0.32f;
                AddSustainedStringNote(leftChannel, rightChannel, barStart, barDuration, triad[noteIdx], sampleRate, 0.13f, pan);
            }
        }

        // ==========================================
        // 4. قيثارة الهارب الساحرة (Magical Plucked Harp Arpeggios)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] triad = chordTriads[bar % chordTriads.Length];

            for (int h = 0; h < 8; h++)
            {
                float harpTime = barStart + h * (barDuration / 8f);
                float freq = triad[h % triad.Length] * 2f;
                float pan = Mathf.Sin(h * 0.8f) * 0.6f;
                AddPluckedHarpNote(leftChannel, rightChannel, harpTime, freq, sampleRate, 0.15f, pan);
            }
        }

        // ==========================================
        // 5. لحن الفلوت الأسطوري (Celtic Forest Pan Flute)
        // ==========================================
        for (int bar = 0; bar < totalBars; bar++)
        {
            float barStart = bar * barDuration;
            float[] melody = fluteMelody[bar % fluteMelody.Length];

            for (int m = 0; m < 4; m++)
            {
                float noteTime = barStart + m * beatDuration;
                float noteFreq = melody[m];
                float noteDuration = beatDuration * 0.88f;
                AddFluteNote(leftChannel, rightChannel, noteTime, noteDuration, noteFreq, sampleRate, 0.22f);
            }
        }

        // ==========================================
        // 6. التنعيم الحلقي المتقاطع (Seamless Loop Crossfade)
        // ==========================================
        int fadeSamples = (int)(sampleRate * 0.25f);
        for (int i = 0; i < fadeSamples; i++)
        {
            float t = (float)i / fadeSamples;
            int tailIdx = totalSamples - fadeSamples + i;

            leftChannel[i] = leftChannel[i] * t + leftChannel[tailIdx] * (1f - t);
            rightChannel[i] = rightChannel[i] * t + rightChannel[tailIdx] * (1f - t);

            leftChannel[tailIdx] = leftChannel[tailIdx] * (1f - t);
            rightChannel[tailIdx] = rightChannel[tailIdx] * (1f - t);
        }

        // دمج وموازنة الصوت النهائي بنظام الستيريو مع Soft Limiter
        float[] interleaved = new float[totalSamples * 2];
        for (int i = 0; i < totalSamples; i++)
        {
            // Soft clipping ناعم للحفاظ على دفء وتفخيم ضربات الطبول دون أي تشويش رقمي
            float l = leftChannel[i];
            float r = rightChannel[i];

            l = Mathf.Clamp(Mathf.Sin(l * 0.88f), -0.98f, 0.98f);
            r = Mathf.Clamp(Mathf.Sin(r * 0.88f), -0.98f, 0.98f);

            interleaved[i * 2] = l;
            interleaved[i * 2 + 1] = r;
        }

        AudioClip themeClip = AudioClip.Create("BeanstalkWar_EpicFantasyBGM", totalSamples, 2, sampleRate, false);
        themeClip.SetData(interleaved, 0);
        return themeClip;
    }

    /// <summary>
    /// طبل التاييكو العملاق الثقيل (Heavy War Taiko Drum) مع ارتداد الصدر وهبوط التردد الزلزالي
    /// </summary>
    private static void AddHeavyTaiko(float[] left, float[] right, float startTime, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.55f * sampleRate);
        float phase = 0f;

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;

            // غلاف الضرب المدوّي (Punch + Body + Sub-bass)
            float malletClick = Mathf.Exp(-t * 50f) * 3.0f; // ضربة المضرب الخشبي الحادة
            float bodyBoom = Mathf.Exp(-t * 8.5f) * 1.35f;  // رنين جلد الطبل القوي
            float subRumble = Mathf.Exp(-t * 5.0f) * 0.75f; // اهتزاز الباص العميق

            // انحدار التردد لمحاكاة شد جلد طبل التاييكو الضخم (180 هرتز إلى 54 هرتز)
            float pitch = 54f + Mathf.Exp(-t * 26f) * 126f;
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float drumWave = Mathf.Sin(phase) + Mathf.Sin(phase * 0.5f) * 0.45f + Mathf.Sin(phase * 2f) * 0.22f;
            float sample = (drumWave * (bodyBoom + subRumble) + malletClick) * volume * 0.46f;

            left[idx] += sample;
            right[idx] += sample;
        }
    }

    /// <summary>
    /// طبل الميدان العسكري الحاد (War Field Snare & Rim Crack)
    /// </summary>
    private static void AddWarSnare(float[] left, float[] right, float startTime, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.28f * sampleRate);
        float phase = 0f;
        var rnd = new System.Random((int)(startTime * 1000));

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;

            float env = Mathf.Exp(-t * 22f);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 35f);

            float pitch = 150f + Mathf.Exp(-t * 45f) * 120f;
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float tone = Mathf.Sin(phase);
            float sample = (tone * 0.65f + noise * 0.55f) * env * volume * 0.38f;

            left[idx] += sample * 0.95f;
            right[idx] += sample * 1.05f;
        }
    }

    /// <summary>
    /// ضربات التوم الحربية الإيقاعية المتتابعة (War Tom-Toms & Fills)
    /// </summary>
    private static void AddWarTom(float[] left, float[] right, float startTime, int sampleRate, float volume, float basePitch, float pan)
    {
        int startSample = (int)(startTime * sampleRate);
        int hitSamples = (int)(0.35f * sampleRate);
        float phase = 0f;

        float panL = Mathf.Clamp01((1f - pan) * 0.5f);
        float panR = Mathf.Clamp01((1f + pan) * 0.5f);

        for (int s = 0; s < hitSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / sampleRate;

            float env = Mathf.Exp(-t * 14f);
            float punch = Mathf.Exp(-t * 45f) * 1.2f;

            float pitch = basePitch + Mathf.Exp(-t * 30f) * (basePitch * 0.85f);
            phase += 2f * Mathf.PI * pitch / sampleRate;

            float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 1.5f) * 0.25f;
            float sample = (wave + punch) * env * volume * 0.35f;

            left[idx] += sample * panL;
            right[idx] += sample * panR;
        }
    }

    /// <summary>
    /// أبواق الحرب الأوركسترالية البطولية (Heroic French Horns / War Brass)
    /// </summary>
    private static void AddHeroicHorn(float[] left, float[] right, float startTime, float duration, float freq, int sampleRate, float volume)
    {
        int startSample = (int)(startTime * sampleRate);
        int noteSamples = (int)(duration * sampleRate);
        float phase1 = 0f;
        float phase2 = 0.25f;

        for (int s = 0; s < noteSamples; s++)
        {
            int idx = startSample + s;
            if (idx >= left.Length) break;

            float t = (float)s / duration;

            // غلاف البوق البطولي الملحمي (Brass Swell & Release)
            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.12f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.15f));
            float env = attack * release;

            phase1 += 2f * Mathf.PI * freq / sampleRate;
            phase2 += 2f * Mathf.PI * (freq * 2.002f) / sampleRate;

            // انسجام نغمي نحاسي دافئ (Brass Harmonics)
            float horn = Mathf.Sin(phase1) * 0.65f + Mathf.Sin(phase2) * 0.35f + Mathf.Sin(phase1 * 3f) * 0.15f;
            float sample = horn * env * volume;

            left[idx] += sample * 0.55f;
            right[idx] += sample * 0.45f;
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

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.22f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.22f));
            float env = attack * release;

            float vibrato = Mathf.Sin(2f * Mathf.PI * 4.8f * ((float)s / sampleRate)) * 0.015f;
            float noteFreq = freq * (1f + vibrato);

            phase1 += 2f * Mathf.PI * noteFreq / sampleRate;
            phase2 += 2f * Mathf.PI * (noteFreq * 1.004f) / sampleRate;

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

            float attack = Mathf.Clamp01((float)s / (sampleRate * 0.09f));
            float release = Mathf.Clamp01((float)(noteSamples - s) / (sampleRate * 0.09f));
            float env = attack * release;

            float vibrato = (s > sampleRate * 0.15f) ? Mathf.Sin(2f * Mathf.PI * 5.2f * ((float)s / sampleRate)) * 0.018f : 0f;
            float noteFreq = freq * (1f + vibrato);

            phase += 2f * Mathf.PI * noteFreq / sampleRate;
            breathPhase += 2f * Mathf.PI * (noteFreq * 2.01f) / sampleRate;

            float flute = Mathf.Sin(phase) * 0.82f + Mathf.Sin(phase * 2f) * 0.15f + Mathf.Sin(breathPhase) * 0.03f;
            float sample = flute * env * volume;

            left[idx] += sample * 0.52f;
            right[idx] += sample * 0.48f;
        }
    }
}
