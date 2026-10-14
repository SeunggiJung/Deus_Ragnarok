using System;
using UnityEngine;

// 효과음 종류. 소리 파일 없이 SfxSynth가 코드로 파형을 합성해서 만든다
public enum Sfx
{
    // 플레이어
    SwingSlash, SwingUpper, SwingThrust, SwingJump, Jump, Dodge, Hurt, PlayerDie,
    // 타격 / 몬스터
    Hit, HitCritical, MonsterSwing, MonsterDie,
    // 보스 / 이펙트
    Roar, Boom, BoomSmall, Fire, Beam, BossDie, Rumble,
    // 보스 패턴 예고 (TelegraphKind 순서)
    WarnPound, WarnGrab, WarnLeap, WarnCharge, WarnGuard, WarnMelee, WarnSpikes, WarnBolt, WarnHeavy,
    // 아이템 / UI
    Chime, Pickup, LevelUp, Potion, Click
}

// 효과음 합성기 : 노이즈 / 사인 / 톱니파에 필터와 엔벨로프를 적용해 스윙(whoosh), 타격(thump), 폭발, 종소리 등을 만든다.
// 각 소리는 (경과 초, 진행률 0~1) → 샘플을 돌려주는 함수로 정의하고, 마지막에 최대 크기를 gain으로 정규화한다
public static class SfxSynth
{
    private const int Rate = 44100;
    private const float TwoPi = Mathf.PI * 2f;

    private static readonly System.Random random = new System.Random(20261014);
    private static float Noise() => (float)(random.NextDouble() * 2.0 - 1.0);

    // 1차 저역 통과 필터 (주파수를 매 샘플 바꿔도 됨)
    private class Lowpass
    {
        private float y;
        public float Process(float x, float freq)
        {
            float a = 1f - Mathf.Exp(-TwoPi * Mathf.Max(1f, freq) / Rate);
            y += a * (x - y);
            return y;
        }
    }

    // 주파수가 변하는 발진기
    private class Osc
    {
        private float phase;
        public float Sin(float freq) { phase += TwoPi * freq / Rate; return Mathf.Sin(phase); }
        public float Saw(float freq) { phase += freq / Rate; phase -= Mathf.Floor(phase); return phase * 2f - 1f; }
    }

    private static float Decay(float t, float k) => Mathf.Exp(-k * t);
    private static float Attack(float t, float a) => Mathf.Clamp01(t / a);

    // 대역 통과 노이즈 : 높은 컷오프 - 낮은 컷오프. 컷오프를 바꿔 가며 "슉" 하는 바람 소리를 만든다
    private class BandNoise
    {
        private readonly Lowpass hi = new Lowpass();
        private readonly Lowpass lo = new Lowpass();
        public float Process(float low, float high)
        {
            float x = Noise();
            return hi.Process(x, high) - lo.Process(x, low);
        }
    }

    private static AudioClip Build(string name, float duration, Func<float, float, float> gen, float peak)
    {
        int count = Mathf.CeilToInt(duration * Rate);
        var data = new float[count];
        float max = 0.0001f;
        for (int i = 0; i < count; i++)
        {
            float v = gen(i / (float)Rate, i / (float)count);
            data[i] = v;
            float abs = Mathf.Abs(v);
            if (abs > max) max = abs;
        }

        // 정규화 + 끝 5ms 페이드아웃 (뚝 끊기는 클릭 방지)
        float scale = peak / max;
        int fade = Mathf.Min(count, Rate / 200);
        for (int i = 0; i < count; i++)
        {
            float f = i >= count - fade ? (count - i) / (float)fade : 1f;
            data[i] *= scale * f;
        }

        var clip = AudioClip.Create(name, count, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Create(Sfx sfx)
    {
        switch (sfx)
        {
            // ───── 플레이어 ─────
            case Sfx.SwingSlash: return Whoosh(sfx, 0.28f, 240f, 1100f, 0.5f, 0.7f, false, 125f, 0.55f);   // 1타 : 대검이 위에서 아래로 가르는 묵직한 소리
            case Sfx.SwingUpper: return Whoosh(sfx, 0.32f, 1200f, 280f, 0.35f, 0.7f, false, 110f, 0.55f);  // 2타 : 올려 베며 낮아지는 소리
            case Sfx.SwingThrust:                                                              // 3타 : 낮은 바람 + 끝에 묵직하게 찌르는 소리
            {
                var band = new BandNoise(); var osc = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 0.36f, (t, n) =>
                {
                    float wind = band.Process(Mathf.Lerp(130f, 450f, n), Mathf.Lerp(380f, 1100f, n)) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, n * 1.3f)) * 3.2f;
                    float thrust = n > 0.5f ? osc.Sin(Mathf.Lerp(130f, 48f, (n - 0.5f) * 2f)) * Decay(t - 0.18f, 11f) * 1.2f : 0f;
                    return wind + thrust + lp.Process(Noise(), 380f) * (n > 0.5f ? Decay(t - 0.18f, 18f) : 0f) * 0.7f;
                }, 0.72f);
            }
            case Sfx.SwingJump: return Whoosh(sfx, 0.36f, 200f, 1600f, 0.55f, 0.78f, false, 100f, 0.75f);   // 점프 공격 : 넓고 무거운 바람
            case Sfx.Jump:
            {
                var osc = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 0.17f, (t, n) =>
                    osc.Sin(Mathf.Lerp(120f, 200f, n)) * Attack(t, 0.012f) * Decay(t, 15f)
                    + lp.Process(Noise(), 520f) * Attack(t, 0.01f) * Decay(t, 20f) * 0.6f, 0.4f);
            }
            case Sfx.Dodge: return Whoosh(sfx, 0.32f, 900f, 220f, 0.45f, 0.4f, soft: true);   // 구르기 : 부드럽게 옷깃 스치는 소리
            case Sfx.Hurt:
            {
                var osc = new Osc(); var saw = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 0.3f, (t, n) =>
                    osc.Sin(Mathf.Lerp(150f, 55f, Mathf.Min(1f, t * 6f))) * Decay(t, 13f)
                    + saw.Saw(Mathf.Lerp(270f, 110f, n)) * Decay(t, 10f) * 0.3f
                    + lp.Process(Noise(), 1300f) * Decay(t, 30f) * 0.5f, 0.8f);
            }
            case Sfx.PlayerDie:
            {
                var osc = new Osc(); var saw = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 1.1f, (t, n) =>
                {
                    float f = Mathf.Lerp(390f, 55f, n * n) * (1f + 0.02f * Mathf.Sin(t * 38f));
                    return osc.Sin(f) * Decay(t, 2.4f) + saw.Saw(f * 0.5f) * Decay(t, 3.2f) * 0.3f + lp.Process(Noise(), 600f) * Decay(t, 6f) * 0.3f;
                }, 0.7f);
            }

            // ───── 타격 / 몬스터 ─────
            case Sfx.Hit:   return Impact(sfx, false);
            case Sfx.HitCritical: return Impact(sfx, true);
            case Sfx.MonsterSwing: return Whoosh(sfx, 0.22f, 300f, 1000f, 0.5f, 0.35f);
            case Sfx.MonsterDie:
            {
                var osc = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 0.34f, (t, n) =>
                    osc.Sin(Mathf.Lerp(230f, 55f, n)) * Decay(t, 7f) + lp.Process(Noise(), 800f) * Decay(t, 13f) * 0.4f, 0.55f);
            }

            // ───── 보스 / 이펙트 ─────
            case Sfx.Roar:
            {
                var a = new Osc(); var b = new Osc(); var band = new BandNoise();
                return Build(sfx.ToString(), 1.2f, (t, n) =>
                {
                    float f = 78f * (1f + 0.06f * Mathf.Sin(t * 26f)) * Mathf.Lerp(1.15f, 0.8f, n);
                    float body = a.Saw(f) + b.Saw(f * 1.51f) * 0.5f;
                    body = (float)Math.Tanh(body * 2.2f);   // 거칠게 찌그러뜨림
                    float growl = band.Process(300f, 1300f) * 2f;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Pow(n, 0.55f));
                    return (body + growl * 0.6f) * env;
                }, 0.85f);
            }
            case Sfx.Boom: return Explosion(sfx, 0.75f, 95f, 32f, 5.5f, 700f, 7f, 0.9f);
            case Sfx.BoomSmall: return Explosion(sfx, 0.36f, 170f, 60f, 11f, 1500f, 18f, 0.6f);
            case Sfx.Fire:
            {
                var band = new BandNoise(); var osc = new Osc();
                return Build(sfx.ToString(), 0.38f, (t, n) =>
                    band.Process(Mathf.Lerp(700f, 200f, n), Mathf.Lerp(3200f, 800f, n)) * Attack(t, 0.01f) * Decay(t, 7f) * 2.5f
                    + osc.Sin(Mathf.Lerp(950f, 250f, n)) * Decay(t, 9f) * 0.3f, 0.5f);
            }
            case Sfx.Beam:
            {
                var a = new Osc(); var b = new Osc(); var band = new BandNoise();
                return Build(sfx.ToString(), 0.95f, (t, n) =>
                {
                    float trem = 0.65f + 0.35f * Mathf.Sin(t * TwoPi * 30f);
                    float body = a.Saw(105f) * 0.6f + b.Saw(158f) * 0.4f + band.Process(1000f, 4500f) * 1.5f;
                    return body * trem * Attack(t, 0.08f) * Mathf.Clamp01((1f - n) * 4f);
                }, 0.6f);
            }
            case Sfx.BossDie:
            {
                var a = new Osc(); var b = new Osc(); var lp = new Lowpass();
                return Build(sfx.ToString(), 2.0f, (t, n) =>
                    a.Sin(Mathf.Lerp(85f, 24f, Mathf.Min(1f, t * 0.8f))) * Decay(t, 1.4f)
                    + b.Saw(Mathf.Lerp(95f, 38f, n)) * Decay(t, 2.2f) * 0.35f
                    + lp.Process(Noise(), 500f) * Decay(t, 2.0f) * 0.9f, 0.9f);
            }
            case Sfx.Rumble:
            {
                var lp = new Lowpass(); var osc = new Osc();
                return Build(sfx.ToString(), 4f, (t, n) =>
                {
                    float env = Mathf.Min(Attack(t, 0.6f), Mathf.Clamp01((1f - n) * 8f));
                    return (lp.Process(Noise(), 75f) * 4f + osc.Sin(38f + 4f * Mathf.Sin(t * 5f)) * 0.6f) * env;
                }, 0.7f);
            }

            // ───── 보스 패턴 예고 ─────
            case Sfx.WarnPound: return Warn(sfx, 0.4f, 150f, 105f, true, 0.5f);       // 낮고 묵직한 울림
            case Sfx.WarnGrab: return Blips(sfx, new[] { 700f, 940f }, 0.1f, 0.5f);   // 날카로운 경고음 두 번
            case Sfx.WarnLeap: return Warn(sfx, 0.4f, 240f, 900f, false, 0.45f);      // 점점 올라가는 도약 준비
            case Sfx.WarnCharge: return Warn(sfx, 0.4f, 190f, 520f, true, 0.5f);      // 으르렁거리며 가속
            case Sfx.WarnGuard: return Bell(sfx, 0.5f, 620f, 0.45f);                  // 금속성 울림
            case Sfx.WarnMelee: return Warn(sfx, 0.16f, 420f, 300f, false, 0.4f);     // 짧은 틱
            case Sfx.WarnSpikes: return Warn(sfx, 0.4f, 95f, 120f, true, 0.45f);      // 지면 울리는 소리
            case Sfx.WarnBolt: return Blips(sfx, new[] { 800f, 1150f, 800f }, 0.07f, 0.4f);
            case Sfx.WarnHeavy: return Warn(sfx, 0.6f, 100f, 320f, true, 0.55f);      // 힘을 모으는 긴 상승음

            // ───── 아이템 / UI ─────
            case Sfx.Chime:   return Arpeggio(sfx, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.13f, 2.6f, 1.9f, 0.7f);   // 부활 : 맑고 긴 종소리
            case Sfx.LevelUp:   // 낮은 단조(A3 C4 E4 A4) 종소리 + 아래에서 받치는 저음
            {
                float[] notes = { 220f, 262f, 330f, 440f };
                var osc = new Osc[notes.Length * 2];
                for (int i = 0; i < osc.Length; i++) osc[i] = new Osc();
                var sub = new Osc();
                return Build(sfx.ToString(), 1.1f, (t, n) =>
                {
                    float v = 0f;
                    for (int i = 0; i < notes.Length; i++)
                    {
                        float local = t - i * 0.11f;
                        if (local < 0f) continue;
                        float env = Attack(local, 0.006f) * Decay(local, 4.2f);
                        v += (osc[i * 2].Sin(notes[i]) + osc[i * 2 + 1].Sin(notes[i] * 2f) * 0.15f) * env;
                    }
                    v += sub.Sin(110f) * Attack(t, 0.08f) * Decay(t, 2.6f) * 0.55f;
                    return v;
                }, 0.6f);
            }
            case Sfx.Pickup:  return Arpeggio(sfx, new[] { 880f, 1320f }, 0.07f, 9f, 0.32f, 0.5f);
            case Sfx.Potion:
            {
                var osc = new Osc();
                return Build(sfx.ToString(), 0.5f, (t, n) =>
                {
                    float local = t % 0.12f;
                    float burst = t < 0.36f ? Mathf.Sin(local / 0.12f * Mathf.PI) : 0f;
                    return osc.Sin(Mathf.Lerp(400f, 950f, local / 0.12f)) * burst;
                }, 0.4f);
            }
            case Sfx.Click:
            {
                var osc = new Osc();
                return Build(sfx.ToString(), 0.07f, (t, n) => osc.Sin(Mathf.Lerp(1100f, 700f, n)) * Decay(t, 55f), 0.4f);
            }
        }
        return Build(sfx.ToString(), 0.05f, (t, n) => 0f, 0.1f);
    }

    #region Recipes
    // 노이즈 바람 소리 : 대역이 fromHz → toHz로 이동. peakAt : 소리가 가장 큰 시점(진행률)
    // bodyHz > 0이면 소리가 가장 큰 시점에 저음 "붕" 소리(공기를 가르는 무게감)를 더함
    private static AudioClip Whoosh(Sfx sfx, float duration, float fromHz, float toHz, float peakAt, float peak, bool soft = false, float bodyHz = 0f, float bodyAmount = 0f)
    {
        var band = new BandNoise();
        var body = new Osc();
        return Build(sfx.ToString(), duration, (t, n) =>
        {
            float fc = Mathf.Lerp(fromHz, toHz, n);
            float env = n < peakAt ? Mathf.Pow(n / peakAt, soft ? 1.2f : 0.7f) : Mathf.Pow(1f - (n - peakAt) / (1f - peakAt), 1.6f);
            float v = band.Process(fc * 0.5f, fc * 1.25f) * env * 3f;
            if (bodyHz > 0f)
            {
                float d = (n - peakAt) / 0.22f;
                v += body.Sin(Mathf.Lerp(bodyHz, bodyHz * 0.55f, n)) * Mathf.Exp(-d * d) * bodyAmount * 1.4f;
            }
            return v;
        }, peak);
    }

    // 타격음 : 저음 thump + 노이즈 타격. 크리티컬은 금속성 울림을 더함
    private static AudioClip Impact(Sfx sfx, bool critical)
    {
        var thump = new Osc(); var lp = new Lowpass(); var hi = new Lowpass();
        var ring1 = new Osc(); var ring2 = new Osc(); var ring3 = new Osc();
        return Build(sfx.ToString(), critical ? 0.45f : 0.18f, (t, n) =>
        {
            float body = thump.Sin(Mathf.Lerp(190f, 55f, Mathf.Min(1f, t * 9f))) * Decay(t, 20f);
            float crack = lp.Process(Noise(), 1800f) * Decay(t, 55f) * 0.8f;
            float v = body + crack;
            if (critical)
            {
                float bright = (Noise() - hi.Process(Noise(), 2500f)) * Decay(t, 45f) * 0.5f;
                float metal = (ring1.Sin(1320f) + ring2.Sin(1980f) * 0.6f + ring3.Sin(2640f) * 0.4f) * Decay(t, 9f) * 0.35f;
                v += bright + metal;
            }
            return v;
        }, critical ? 0.9f : 0.8f);
    }

    private static AudioClip Explosion(Sfx sfx, float duration, float fromHz, float toHz, float sineDecay, float noiseHz, float noiseDecay, float peak)
    {
        var osc = new Osc(); var lp = new Lowpass();
        return Build(sfx.ToString(), duration, (t, n) =>
            osc.Sin(Mathf.Lerp(fromHz, toHz, Mathf.Min(1f, t * 4f))) * Decay(t, sineDecay)
            + lp.Process(Noise(), noiseHz) * Decay(t, noiseDecay) * 0.9f, peak);
    }

    // 예고음 : 주파수가 f0 → f1로 움직이는 소리 (growl이면 톱니파로 거칠게)
    private static AudioClip Warn(Sfx sfx, float duration, float f0, float f1, bool growl, float peak)
    {
        var osc = new Osc(); var sub = new Osc();
        return Build(sfx.ToString(), duration, (t, n) =>
        {
            float f = Mathf.Lerp(f0, f1, n);
            float env = Attack(t, 0.03f) * Mathf.Clamp01((1f - n) * 5f);
            float tone = growl ? osc.Saw(f) * 0.6f + sub.Sin(f * 0.5f) * 0.6f : osc.Sin(f);
            return tone * env * (0.8f + 0.2f * Mathf.Sin(t * TwoPi * 18f));
        }, peak);
    }

    // 짧은 삑 소리 여러 번 (경고음)
    private static AudioClip Blips(Sfx sfx, float[] freqs, float each, float peak)
    {
        var osc = new Osc();
        return Build(sfx.ToString(), each * freqs.Length + 0.05f, (t, n) =>
        {
            int index = Mathf.Min(freqs.Length - 1, (int)(t / each));
            float local = t - index * each;
            float env = local < each ? Mathf.Sin(local / each * Mathf.PI) : 0f;
            return osc.Sin(freqs[index]) * env;
        }, peak);
    }

    // 금속 종소리
    private static AudioClip Bell(Sfx sfx, float duration, float freq, float peak)
    {
        var a = new Osc(); var b = new Osc(); var c = new Osc();
        return Build(sfx.ToString(), duration, (t, n) =>
            (a.Sin(freq) + b.Sin(freq * 2.76f) * 0.5f + c.Sin(freq * 5.4f) * 0.25f) * Attack(t, 0.003f) * Decay(t, 7f), peak);
    }

    // 음을 차례로 울리는 종소리 아르페지오. decay : 음별 감쇠 속도, tail : 마지막 음 이후 여운
    private static AudioClip Arpeggio(Sfx sfx, float[] freqs, float step, float decay, float duration, float peak)
    {
        var osc = new Osc[freqs.Length * 2];
        for (int i = 0; i < osc.Length; i++) osc[i] = new Osc();
        return Build(sfx.ToString(), duration, (t, n) =>
        {
            float v = 0f;
            for (int i = 0; i < freqs.Length; i++)
            {
                float local = t - i * step;
                if (local < 0f) continue;
                float env = Attack(local, 0.004f) * Decay(local, decay);
                v += (osc[i * 2].Sin(freqs[i]) + osc[i * 2 + 1].Sin(freqs[i] * 2f) * 0.3f) * env;
            }
            return v;
        }, peak);
    }
    #endregion
}
