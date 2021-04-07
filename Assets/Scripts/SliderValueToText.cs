using UnityEngine;
using UnityEngine.UI;

public class SliderValueToText : MonoBehaviour {
    public Slider sliderUI;
    private Text _textSliderValue;

    void Start (){
        _textSliderValue = GetComponent<Text>();
        ShowSliderValue();
    }

    public void ShowSliderValue () {
        _textSliderValue.text = sliderUI.value.ToString("n2");
    }
}
