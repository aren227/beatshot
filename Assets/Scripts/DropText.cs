// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class DropText : MonoBehaviour
// {
//     GameObject dropObject;

//     public Color dropColor = Color.blue;
//     public Vector2 dropOffset = new Vector2(6, -6);

//     void Awake() {
//         dropObject = new GameObject("Drop Text");

//         dropObject.transform.SetParent(transform, false);

//         dropObject.AddComponent<RectTransform>();

//         Text text = GetComponent<Text>();
//         Text dropText = dropObject.AddComponent<Text>();

//         dropText.text = text.text;
//         dropText.fontSize = text.fontSize;

//         dropText.color = dropColor;

//         Canvas canvasForSorting = dropObject.AddComponent<Canvas>();

//         canvasForSorting.overrideSorting = true;
//         canvasForSorting.sortingOrder = -1;
//     }

//     void LateUpdate() {
//         RectTransform rectTransform = GetComponent<RectTransform>();
//         RectTransform dropRectTransform = dropObject.GetComponent<RectTransform>();

//         dropRectTransform.anchoredPosition = rectTransform.anchoredPosition;
//         dropRectTransform.offsetMin = rectTransform.offsetMin;
//         dropRectTransform.offsetMax = rectTransform.offsetMax;
//         dropRectTransform.anchorMin = rectTransform.anchorMin;
//         dropRectTransform.anchorMax = rectTransform.anchorMax;
//         dropRectTransform.sizeDelta = new Vector2(0, 100);

//         dropRectTransform.anchoredPosition = rectTransform.anchoredPosition;
//     }
// }
