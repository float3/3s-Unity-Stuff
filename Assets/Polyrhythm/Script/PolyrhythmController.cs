using System.Globalization;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class PolyrhythmController : UdonSharpBehaviour
{
	public Material _audio;
	public Material _visualizer;
	public Material _LCM;
	public Material _polyrhythm;

	public Toggle _root;
	public Toggle _minSecond;
	public Toggle _majSecond;
	public Toggle _minThird;
	public Toggle _majThird;
	public Toggle _Fourth;
	public Toggle _Tritone;
	public Toggle _Fifth;
	public Toggle _minSixth;
	public Toggle _majSixth;
	public Toggle _minSeventh;
	public Toggle _majSeventh;
	public Toggle _octave;

	public Toggle _mode;

	public Slider _rootFreq;
	public Slider _volume;

	public Slider _x;
	public Slider _y;

	public TextMeshProUGUI _currentPolyrhythm;

	public TextMeshProUGUI _currentChord;
	public TextMeshProUGUI _currentRoot;


	void Start()
	{
		Debug.Log(
			"https://www.shadertoy.com/view/7tV3WV \n https://www.youtube.com/watch?v=-tRAkWaeepg \n https://www.youtube.com/watch?v=JiNKlhspdKg \n https://www.youtube.com/watch?v=_gCJHNBEdoc \n https://www.youtube.com/watch?v=n9l7HqhJugA");
	}

	// void setFloats(Material m)
	// {
	// 	m.SetFloat("_hasPrime", _root.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMinSecond", _minSecond.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMajSecond", _majSecond.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMinThird", _minThird.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMajThird", _majThird.isOn ? 1 : 0);
	// 	m.SetFloat("_hasFourth", _Fourth.isOn ? 1 : 0);
	// 	m.SetFloat("_hasTritone", _Tritone.isOn ? 1 : 0);
	// 	m.SetFloat("_hasFifth", _Fifth.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMinSixth", _minSixth.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMajSixth", _majSixth.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMinSeventh", _minSeventh.isOn ? 1 : 0);
	// 	m.SetFloat("_hasMajSeventh", _majSeventh.isOn ? 1 : 0);
	// 	m.SetFloat("_hasOctave", _octave.isOn ? 1 : 0);
	// 	m.SetFloat("_polyrhythm", _mode.isOn ? 1 : 0);
	// }

	public void _Polyrhythm()
	{
		_audio.SetFloat("_hasPrime", _root.isOn ? 1 : 0);
		_audio.SetFloat("_hasMinSecond", _minSecond.isOn ? 1 : 0);
		_audio.SetFloat("_hasMajSecond", _majSecond.isOn ? 1 : 0);
		_audio.SetFloat("_hasMinThird", _minThird.isOn ? 1 : 0);
		_audio.SetFloat("_hasMajThird", _majThird.isOn ? 1 : 0);
		_audio.SetFloat("_hasFourth", _Fourth.isOn ? 1 : 0);
		_audio.SetFloat("_hasTritone", _Tritone.isOn ? 1 : 0);
		_audio.SetFloat("_hasFifth", _Fifth.isOn ? 1 : 0);
		_audio.SetFloat("_hasMinSixth", _minSixth.isOn ? 1 : 0);
		_audio.SetFloat("_hasMajSixth", _majSixth.isOn ? 1 : 0);
		_audio.SetFloat("_hasMinSeventh", _minSeventh.isOn ? 1 : 0);
		_audio.SetFloat("_hasMajSeventh", _majSeventh.isOn ? 1 : 0);
		_audio.SetFloat("_hasOctave", _octave.isOn ? 1 : 0);
		_audio.SetFloat("_polyrhythm", _mode.isOn ? 1 : 0);

		_visualizer.SetFloat("_hasPrime", _root.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMinSecond", _minSecond.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMajSecond", _majSecond.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMinThird", _minThird.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMajThird", _majThird.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasFourth", _Fourth.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasTritone", _Tritone.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasFifth", _Fifth.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMinSixth", _minSixth.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMajSixth", _majSixth.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMinSeventh", _minSeventh.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasMajSeventh", _majSeventh.isOn ? 1 : 0);
		_visualizer.SetFloat("_hasOctave", _octave.isOn ? 1 : 0);

		_LCM.SetFloat("_hasPrime", _root.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMinSecond", _minSecond.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMajSecond", _majSecond.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMinThird", _minThird.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMajThird", _majThird.isOn ? 1 : 0);
		_LCM.SetFloat("_hasFourth", _Fourth.isOn ? 1 : 0);
		_LCM.SetFloat("_hasTritone", _Tritone.isOn ? 1 : 0);
		_LCM.SetFloat("_hasFifth", _Fifth.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMinSixth", _minSixth.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMajSixth", _majSixth.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMinSeventh", _minSeventh.isOn ? 1 : 0);
		_LCM.SetFloat("_hasMajSeventh", _majSeventh.isOn ? 1 : 0);
		_LCM.SetFloat("_hasOctave", _octave.isOn ? 1 : 0);

		_currentChord.text = GetChordName();
	}

	public void _Mode()
	{
		_audio.SetFloat("_Polyrhythm", _mode.isOn ? 1 : 0);
	}

	private string GetChordName()
	{
		string chordName = ""; //unused for now

		if (_majThird.isOn)
		{
			if (_majSeventh.isOn) chordName += "maj7";
			else if (_minSeventh.isOn) chordName += "7";
			else if (_majSixth.isOn)
			{
				chordName += "6";
				if (_majSecond.isOn)
				{
					chordName += "/9";
				}
			}
			else if (_minSixth.isOn) chordName += "aug"; //aug
			else chordName += "maj";
		}

		else if (_minThird.isOn)
		{
			if (_Tritone.isOn && !_Fifth.isOn) chordName += "dim";
			else chordName += "min";

			if (_minSeventh.isOn)
			{
				chordName += "7"; // min7
				if (_Tritone.isOn)
				{
					chordName = "min7b5"; // min7b5
				}
			}
			else if (_majSeventh.isOn) chordName += "(maj7)"; // min(maj7)
		}
		else if (_Fifth.isOn) chordName += "5"; // C5

		if (_majSecond.isOn) chordName += "sus2";

		if (_Fourth.isOn) chordName += "sus4";

		return chordName;
	}

/*
	private string GetChordNameRewrite()
	{
		bool maj;
		bool dom;
		bool aug;

		bool min;
		bool dim;

		string rootNote = "C";
		string chord = "";

		bool incomplete;

		maj = _majThird.isOn && !_minSixth.isOn && !_minThird.isOn && !_minSeventh.isOn;
		dom = _majThird.isOn && !_minSixth.isOn && !_minThird.isOn && _minSeventh.isOn;
		aug = _majThird.isOn && _minSixth.isOn && !_minThird.isOn;
		min = _minThird.isOn && !_Tritone.isOn && !_majThird.isOn;
		dim = _minThird.isOn && _Tritone.isOn && !_majThird.isOn;

		if (min && _minSeventh)
		{
			chord = "min7";
		}
		

		return rootNote + chord;
	}
*/

	public void _VolumeChanged()
	{
		_audio.SetFloat("_Volume", _volume.value);
		_visualizer.SetFloat("_Volume", _volume.value);
	}

	public void _PolyRhythmChanged()
	{
		_polyrhythm.SetFloat("_x", _x.value);
		_polyrhythm.SetFloat("_y", _y.value);
		_currentPolyrhythm.text = _x.value + ":" + _y.value;
	}

	public void _RootChanged()
	{
		float root = 0.859375f * Mathf.Pow(2, _rootFreq.value * 10);

		_currentRoot.text = root.ToString(CultureInfo.CurrentCulture) + " Hz";

		_audio.SetFloat("_Root", root);
		_visualizer.SetFloat("_Root", root);
	}
}