using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatInputField : MonoBehaviour {

	public InputField inputField;
	public ChatManager chatManager;

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(inputField.text))
		{
			chatManager.Send(inputField.text);
			inputField.text = string.Empty;
		}
	}

}
