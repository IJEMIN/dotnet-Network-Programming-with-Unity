using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageText : MonoBehaviour {

	public Text messageText;

	public void SetUp(string message)
	{
		messageText.text = message;
	}

}
