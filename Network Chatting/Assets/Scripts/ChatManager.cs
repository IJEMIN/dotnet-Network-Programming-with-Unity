using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public class ChatManager : MonoBehaviour {

	List<GameObject> messages = new List<GameObject>();

	public TransportTCP m_transport; // 네트워크 연결을 담당

	public MessageText m_messageTextPrafab; // 말풍선

	public Transform m_messageHolder; // 말풍선을 붙일곳

	public string m_hostAddress = "127.0.0.1";

	public int m_port = 50666;

	private bool m_isHost; // 방장(서버)

	public void UpdateHostAddress(string newAddress)
	{
		m_hostAddress = newAddress;
	}

	// 매프레임마다 패킷큐를 긁어와서 새로운 메시지를 추가
	IEnumerator UpdateMessage()
	{
		while(true)
		{
			byte[] buffer = new byte[1400];

			int recvSize = m_transport.Receive(ref buffer,buffer.Length);

			if(recvSize > 0)
			{
				string message = System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log("Receive: " + message);
				AddMessageText(message);
			}
			yield return null;
		}
	}

	// 텍스트를 넘겨주면 프리팹을 찍어내서 말풍선을 추가
	void AddMessageText(string message)
	{
		MessageText instance = Instantiate(m_messageTextPrafab,m_messageHolder);

		messages.Add(instance.gameObject);
		instance.SetUp(message);
	}


	// 방 만들기 (서버 역할하기)
	public void CreateRoom()
	{
		if(m_transport.StartServer(m_port,1))
		{
			m_isHost = true;
			StartCoroutine("UpdateMessage");
		}
		else
		{
			Debug.LogError("Create a Room Failed");
		}
	}

	// 클라이언트가 미리 만들어진 방에 가는것
	public void JoinRoom()
	{
		if(m_transport.Connect(m_hostAddress,m_port))
		{
			m_isHost = false;
			StartCoroutine("UpdateMessage");
		}
		else
		{
			Debug.LogError("Join Room Failed");
		}
	}

	public void Leave()
	{
		while(messages.Count > 0)
		{
			var instance = messages[0];
			messages.RemoveAt(0);
			Destroy(instance);
		}

		if(m_isHost)
		{
			m_transport.StopServer();
		}
		else
		{
			m_transport.Disconnect();
		}

		StopCoroutine("UpdateMessage");
	}

	void OnApplicationQuit()
	{
		Leave();
	}

	public void Send(string message)
	{
		message = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message;

		byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);

		m_transport.Send(buffer,buffer.Length);

		AddMessageText(message);
	}

}
