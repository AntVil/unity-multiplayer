using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlackScreen : MonoBehaviour
{
    public Image blackScreen;
    public TextMeshProUGUI blackScreenText;

    public void SetText(string text)
    {
        blackScreenText.text = text;
    }

    public void SetAlpha(float alpha)
    {
        Color tempColor = blackScreen.color;
        tempColor.a = alpha;
        blackScreen.color = tempColor;

        tempColor = blackScreenText.color;
        tempColor.a = alpha;
        blackScreenText.color = tempColor;
    }
}
