using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatText : MonoBehaviour {

	public Text commentText;
	public void SetUp(string message)
	{
		commentText.text = message;
	}

}
