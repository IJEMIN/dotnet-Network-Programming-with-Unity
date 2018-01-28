using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices; // https://msdn.microsoft.com/ko-kr/library/system.runtime.interopservices(v=vs.110).aspx

// 이 타입의 목적: 물흐르듯이 패킷을 쌓고 꺼내올수 있는 큐
public class PacketQueue
{
	// 대응되는 패킷이 정확히 스트림에서 어디 인덱스에 존재하는지 지정
	struct PacketInfo
	{
		public int offset; // 이 데이터가 기록되기 시작한 지점
		public int size; // 이 데이터의 사이즈
	}

	// MemoryStream 은 데이터를 연속적으로 쌓고 빼낼수 있는 스트림 버퍼
	// MemoryStream 은 디스크나 네트워크 연결이 아니라 메모리 상에 데이터를 저장하는 스트림 버퍼
	// 데이터의 끊김이 없으므로 패킷으로는 다룰수 없다
	private MemoryStream m_streamBuffer;


	private List<PacketInfo> m_waitingPackets; // 아직 디큐하지 못하고 쌓인 패킷들

	private int m_cursor = 0; // 버퍼가 채워져 커서가 밀려난 지점, 데이터를 채울때 여기서 부터 채우면 됨

	private Object lockObj = new Object(); // 데드락 방지용 단순 참고자

	// 초기화
	public PacketQueue()
	{
		m_streamBuffer = new MemoryStream();
		m_waitingPackets = new List<PacketInfo>();
	}

	// 큐에 바이트 데이터 추가
	public int Enqueue(byte[] data, int size)
	{
		PacketInfo info = new PacketInfo();

		info.offset = m_cursor; // 스트림내에서 이 데이터를 찾으려면 어디서 부터 읽으면 되는지 기록
		info.size = size;

		lock(lockObj) // 데드락 방지
		{
			// 패킷 저장 정보를 보존
			m_waitingPackets.Add(info);

			// 패킷 데이터를 추가

			m_streamBuffer.Position = m_cursor; // 스트림의 위치를 마지막으로 갱신한 커서 위치로.
			m_streamBuffer.Write(data,0,size); // 현재 위치에서 사이즈만큼 버퍼를 쌓기
			m_streamBuffer.Flush(); // 적용

			m_cursor += size; // 커서를 방금 추가한 패킷 만큼 오른쪽으로 이동
		}

		return size; // 추가된 데이터의 사이즈를 리턴
	}

	// 버퍼를 맡기면 채워서 넘겨줌
	public int Dequeue(ref byte[] buffer, int size)
	{
		// 패킷 리스트에 남아있는게 없다면 종료
		if(m_waitingPackets.Count <= 0)
		{
			return -1;
		}

		int recvSize = 0;

		// 데드락 방지
		lock(lockObj) {
			// 가장 마지막에 추가된 패킷부터 가져온다
			PacketInfo info = m_waitingPackets[0];

			// 저장된 패킷 이상의 사이즈를 긁어올순 없음
			int dataSize = Math.Min(size, info.size);

			// 읽기 시작할 위치
			m_streamBuffer.Position = info.offset;
			// 현재 커서 위치에서 dataSize 만큼 오른쪽만큼 이동한 지점까지를 영역으로
			// 데이터를 긁어와 입력으로 들어온 buffer 를 채움
			recvSize = m_streamBuffer.Read(buffer, 0, dataSize);

			// 패킷을 꺼냈으므로 꺼낸 패킷에 대한 패킷 기록을 리스트에서 삭제
			if(recvSize > 0)
			{
				m_waitingPackets.RemoveAt(0);
			}

			// 모든 큐 데이터를 꺼냈을때는 스트림을 다시 제로포인트로 클리어해서 메모리를 절약
			if(m_waitingPackets.Count == 0)
			{
				Clear();
				m_cursor = 0;
			}

		}
		return recvSize;

	}

	// 스트림버퍼의 커서를 초기화. 큐와 메모리 클리어
	public void Clear()
	{
		// 스트림 버퍼의 남은 데이터를 긁어서
		byte[] buffer = m_streamBuffer.GetBuffer();
		// 해당 데이터를 정렬함
		Array.Clear(buffer,0,buffer.Length);

		// 스트림 버퍼의 커서와 길이를 제로로 되돌리기
		m_streamBuffer.Position = 0;
		m_streamBuffer.SetLength(0);
	}

}