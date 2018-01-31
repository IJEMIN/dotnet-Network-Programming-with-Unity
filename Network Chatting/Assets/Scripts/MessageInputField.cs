using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageInputField : MonoBehaviour {

	public Chat chat;
	public InputField inputField;

	void Update () {
		if(Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(inputField.text))
		{
			chat.Send(inputField.text);
			inputField.text = string.Empty;
		}
	}
}
