using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace Server
{
     class Packet
    {
        //Подстановка сообщения в очередь на отправку клиенту
        public void SendPacket(Packet packet, IPEndPoint to) 
        {
            _outgoingMessages.Enqueue(new Tuple<Packet, IPEndPoint>(packet, to));
        }

        //Подстановка сообщения "Прощай" в очередь на отправку клиенту
        public void GameEnd(IPEndPoint to)
        {
            _gameEndPacketTo.Enqueue(to);
        }

        //Передача позиции мяча клиенту
        public void BallPosition
        {
            _ball.Position.X.Enqueue(to);
            _ball.Position.Y.Enqueue(to);
        }

        //Передача позиции палки клиенту
        public void PaddlePosition(Player)
        {
            //Правому
            if (!Player == Right){
                _paddle.RightPlayer.Position.X.Enqueue(Player);
                _paddle.RightPlayer.Position.Y.Enqueue(Player);
            }
            
            //Левому
            if (!Player == Left){
                _paddle.LeftPlayer.Position.X.Enqueue(Player);
                _paddle.LeftPlayer.Position.Y.Enqueue(Player);
            }
        }
        
        //Передача оставшегося времени клиенту
        public void TimeToEnd
        {
            _timeEnd.Enqueue(to);
        }
        
        //Передача текущего счета клиенту
        public void Score
        {
            _score.Enqueue(to);
        }

        //Передача списка лучших игроков клиенту
        public void HighScore
        {
            _highScore.Enqueue(to);
        }
    }
}
