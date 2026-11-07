// Web Audio back end for SoundEngine in WebGL players, where OnAudioFilterRead is not available.
// The graph follows web-threejs/src/audio/AudioEngine.ts; SoundEngine still decides every target and tone,
// this file only plays them. Browsers start audio suspended, so the context resumes on the first key or touch.
mergeInto(LibraryManager.library, {
  UjianAudio_Init: function () {
    if (window.ujianAudio) return;
    var Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) return;
    var ctx = new Ctx();
    var a = { ctx: ctx };
    window.ujianAudio = a;

    function gain(v, to) { var g = ctx.createGain(); g.gain.value = v; g.connect(to); return g; }
    function osc(type, to) { var o = ctx.createOscillator(); o.type = type; o.connect(to); o.start(); return o; }
    function filter(type, freq, q, to) {
      var f = ctx.createBiquadFilter(); f.type = type; f.frequency.value = freq; f.Q.value = q; f.connect(to); return f;
    }

    a.master = gain(0, ctx.destination);

    // Engine: saw plus a square an octave down through a low-pass.
    a.engineGain = gain(0, a.master);
    a.engineFilter = filter('lowpass', 300, 0.7, a.engineGain);
    a.engine = osc('sawtooth', a.engineFilter);
    a.sub = osc('square', a.engineFilter);

    // Wind (band-passed noise) and rain (high-passed noise) share one looping noise buffer.
    var buf = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    var data = buf.getChannelData(0);
    for (var i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
    var noise = ctx.createBufferSource();
    a.noise = noise;
    noise.buffer = buf;
    noise.loop = true;
    a.wind = gain(0, a.master);
    a.rain = gain(0, a.master);
    noise.connect(filter('bandpass', 400, 0.4, a.wind));
    noise.connect(filter('highpass', 1500, 0.7, a.rain));
    noise.start();

    // Siren and the two-tone horn.
    a.sirenGain = gain(0, a.master);
    a.siren = osc('triangle', a.sirenGain);
    a.hornGain = gain(0, a.master);
    var hornFilter = filter('lowpass', 2200, 0.7, a.hornGain);
    a.hornA = osc('square', hornFilter);
    a.hornB = osc('square', hornFilter);

    a.resume = function () {
      if (ctx.state === 'suspended' && !document.hidden) ctx.resume().catch(function () {});
    };
    a.visibility = function () {
      if (document.hidden && ctx.state === 'running') ctx.suspend().catch(function () {});
      else if (!document.hidden) a.resume();
    };
    ['keydown', 'pointerdown', 'touchstart', 'focus'].forEach(function (e) { window.addEventListener(e, a.resume, true); });
    document.addEventListener('visibilitychange', a.visibility);
  },

  UjianAudio_Dispose: function () {
    var a = window.ujianAudio;
    if (!a) return;
    ['keydown', 'pointerdown', 'touchstart', 'focus'].forEach(function (e) { window.removeEventListener(e, a.resume, true); });
    document.removeEventListener('visibilitychange', a.visibility);
    a.master.gain.cancelScheduledValues(a.ctx.currentTime);
    a.master.gain.setValueAtTime(0, a.ctx.currentTime);
    [a.engine, a.sub, a.siren, a.hornA, a.hornB, a.noise].forEach(function (node) {
      node.stop();
      node.disconnect();
    });
    if (a.ctx.state !== 'closed') a.ctx.close().catch(function () {});
    delete window.ujianAudio;
  },

  UjianAudio_Set: function (master, engineFreq, engineCut, engineGain, sirenFreq, sirenGain, hornA, hornB, hornGain, wind, rain) {
    var a = window.ujianAudio;
    if (!a) return;
    var t = a.ctx.currentTime;
    // Time constants match the per-sample smoothing of the desktop mix (40, 10 and 2 Hz).
    var fast = 0.004, mid = 0.016, slow = 0.08;
    a.master.gain.setTargetAtTime(master, t, fast);
    a.engine.frequency.setTargetAtTime(engineFreq, t, mid);
    a.sub.frequency.setTargetAtTime(engineFreq * 0.5, t, mid);
    a.engineFilter.frequency.setTargetAtTime(engineCut, t, mid);
    a.engineGain.gain.setTargetAtTime(engineGain, t, mid);
    a.siren.frequency.setTargetAtTime(sirenFreq, t, fast);
    a.sirenGain.gain.setTargetAtTime(sirenGain, t, mid);
    a.hornA.frequency.setTargetAtTime(hornA, t, fast);
    a.hornB.frequency.setTargetAtTime(hornB, t, fast);
    a.hornGain.gain.setTargetAtTime(hornGain, t, fast);
    a.wind.gain.setTargetAtTime(wind, t, slow);
    a.rain.gain.setTargetAtTime(rain, t, slow);
  },

  // wave: 0 sine, 1 square, 2 saw, 3 triangle (SoundEngine.Wave).
  UjianAudio_Tone: function (freq, delay, dur, wave, volume, slideTo) {
    var a = window.ujianAudio;
    if (!a) return;
    var ctx = a.ctx;
    var at = ctx.currentTime + delay;
    var o = ctx.createOscillator();
    o.type = ['sine', 'square', 'sawtooth', 'triangle'][wave];
    o.frequency.setValueAtTime(freq, at);
    if (slideTo > 0) o.frequency.exponentialRampToValueAtTime(slideTo, at + dur);
    var g = ctx.createGain();
    g.gain.setValueAtTime(0, at);
    g.gain.linearRampToValueAtTime(volume, at + 0.01);
    g.gain.exponentialRampToValueAtTime(0.0001, at + dur);
    o.connect(g);
    g.connect(a.master);
    o.start(at);
    o.stop(at + dur + 0.02);
  },
});
