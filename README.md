# Документация к проекту
Данный проект включает в себя серверную и клиентскию часть игры Пинг-Понг написанную на сокетах.


# Оглавление

- **Глава I**
  - [Пакет](#Пакет)
  - [Установление соединения](#Установление-соединения)
- **Глава II**
  - [Начало игры](#Начало-игры)
  - [Процесс игры](#Процесс-игры)
  - [Окончание игры](#Окончание-игры)
- **Глава III**
  - [Игровые механики](#Игровые-механики)
- **Заключение**
  - [Библиотеки](#Библиотеки)

 
## Пакет

Первые 4 байта отдаются _PacketType_, который указывает на тип пакета.
Следующие 8 байт выделяются для _timestamp_ - это время отправленного пакета. После
этого следует массив _data_ с информацией, которую мы захотим загрузить (к примеру
массив массивов топ 10). В итоге получается, что минимальный размер пакета может составлять
4+8=12 байт.

```c#
public class Packet
{
  public byte[] data = new byte[0];  //data that is used as an answer to someone
  public long timestamp; //time when packet was created
  public PacketType type; //type of packet
}
```
### Типы пакетов (PacketType)

Как видно у пакетов есть тип, который будет помогать с определением действий сервера или
клиента.

```c#
public enum PacketType : uint
{
  AcceptJoin = 1,
  IsHereAck, //server acknowledges client`s state
  GameStart,
  GameState,
  GameEnd = 5,
  //!server
  RequestJoin = 6,
  IsHere,
  JoinAck,
  GameStartAck,
  PaddlePosition //position of paddle
}
```
### Реализация пакетов
- **Серверные**
  - [AcceptJoin](#AcceptJoin)
  - [IsHereAck](#IsHereAck)
  - [GameStart](#GameStart)
  - [GameState](#GameState)
- **Клиентсие**
  - RequestJoin
  - IsHere
  - JoinAck
  - GameStartAck
  - PaddlePosition
- **Другие**
  - [GameEnd](#GameEnd)

#### AcceptJoin
AcceptJoin – сервер отправляет клиенту в ответ на запрос о подключении. Содержит в себе информацию о стороне игрока (левый/правый).
```c#
public class AcceptJoin : Packet
    {
        // Paddle side
        public PaddleSide Side
        {
            get { return (PaddleSide)BitConverter.ToUInt32(data, 0); }
            set { data = BitConverter.GetBytes((uint)value); }
        }

        public AcceptJoin() : base(PacketType.AcceptJoin)
        {
            data = new byte[sizeof(PaddleSide)];
            Side = PaddleSide.None; //default value
        }
        public AcceptJoin(byte[] bytes) : base(bytes) { }
    }
 ```
#### IsHereAck
IsHereAck – сервер отправляет клиенту, для подтверждения, что клиент все еще подключен.
```c#
 public class IsHereAck : Packet
    {
        public IsHereAck() : base(PacketType.IsHereAck) { }
    }
```
```c#
case PacketType.IsHere:
  IsHereAck hap = new IsHereAck();
  Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
  sendTo(player, hap);
  player.LastPacketReceivedTime = mes.recvTime;
break;
```
#### GameStart
GameStart – сервер отправляет клиенту для уведомления последнего о начале игры.
```c#
public class GameStart : Packet
    {
        public GameStart() : base(PacketType.GameStart) { }
    }
```
#### GameStatePacket
GameStatePacket – сервер передает клиенту положение мяча, палочек и счет.
```c#
public GameStatePacket()
            : base(PacketType.GameState)
        {
            // Allocate data for the payload (we really shouldn't hardcode this in...)
            data = new byte[24];

            // Set default data
            LeftY = 0;
            RightY = 0;
            BallPosition = new Vector2();
            LeftScore = 0;
            RightScore = 0;
        }
```
#### GameEnd
GameEnd – отправляет сервер клиенту или наоборот, чтобы уведомить другого игрока, что игра окончена.
```c#
public class EndGame : Packet
    {
        public EndGame() : base(PacketType.GameEnd) { }
    }
```
## Установление соединения
- Клиент запрашивает подключение. 
- Сервер отвечает согласием и передает игровую сторону клиента (право/лево). 
- Клиент отвечает, что получил пакет с установленной для него стороной игры.
![Установление соединения](Documentation/TCP_handshake.png)

Все три  пакета подключения содержат в себе timestamp, чтобы определять, когда был отправлен пакет. 
```c#
public class Packet
    {
        public byte[] data = new byte[0];  //data that is used as an answer to someone
        public long timestamp; //time when packet was created
        public PacketType type;
     }
```
Если сервер/клиен хочет повторно отправить пакет, то это возможно только после таймаута в 20 секунд, чтобы не спамить и параллельно проверять потерю соединения. 
```c#
private Server server;
private TimeSpan Timeout = TimeSpan.FromSeconds(20);
```
Благодаря очереди, последовательной отправке сообщений, расположенной на сервере, получается добиться правильной последовательности исходящих и выходящих пакетов.
```c#
private ConcurrentQueue<Message> inMessages = new ConcurrentQueue<Message>();
private ConcurrentQueue<Tuple<Packet, IPEndPoint>> outMessages = new ConcurrentQueue<Tuple<Packet, IPEndPoint>>();
private ConcurrentQueue<IPEndPoint> send_EndGame_packetTo = new ConcurrentQueue<IPEndPoint>();
```
## Начало игры
Сервер не начнет игру, пока не будут подключены два клиента.
Когда оба клиента подключены, сервер отправляет обоим клиентам сообщение GameStart.
```c#
if (leftPlayer.pickedSide && rightPlayer.pickedSide)
                            {
                                GameStarting(leftPlayer, new TimeSpan());
                                GameStarting(rightPlayer, new TimeSpan());

                                State.var = FieldState.GameStarting;
                            }
```
```c#
private void GameStarting(Player player, TimeSpan retryTimeout)
        {
            if (player.ready)
                return;

            if (DateTime.Now >= (player.LastPacketSentTime.Add(retryTimeout)))
            {
                GameStart gsp = new GameStart();
                sendTo(player, gsp);
            }
        }
```
Оба клиента должны ответить GameStartAck прежде, чем сервер отправит какие-либо сообщения GameState.
## Процесс игры
Несколько раз в секунду клиенты и сервер отправляют друг другу информацию о своем текущем состоянии.
```c#
case PacketType.IsHere:
 IsHereAck hap = new IsHereAck();
 Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
 sendTo(player, hap);
 player.LastPacketReceivedTime = mes.recvTime;
break;
```
Клиент отправляет на сервер только PaddlePosition в виде числа с плавающей запятой.
Сервер отправляет обоим клиентам пакет GameState, содержащий позиции ракетки, мяча и текущий счет.
```c#
private void sendGameState(Player player, TimeSpan resendTimeout)
{
  if (DateTime.Now >= (player.LastPacketSentTime.Add(resendTimeout)))
  {
    //GET: game state
    //set all objs
    GameStatePacket gsp = new GameStatePacket
    {
      LeftY = leftPlayer.paddle.Position.Y,
      RightY = rightPlayer.paddle.Position.Y,
      BallPosition = ball.Position,
      LeftScore = leftPlayer.paddle.Score,
      RightScore = rightPlayer.paddle.Score
    };

    sendTo(player, gsp);
  }
}
```
## Окончание игры
Когда игра заканчивается отправляется пакет EndGame. Сервер отправят клиентам, что игра окончена и также отправляет обновленный топ 10.
*Пример кода EndGame, а также как сервер отправляет топ 10*

Клиент же просто отправляет уведомление об окончании игры, если сам инициировал выход не закончив игру.
*Пример кода где это написано*

# Игровые механики
*Показать рисунок как выглядит коллайдер ракетки и мяча и описать, что происходит в случаях когда шар касается определенного коллайдера*
*Пример кода коллайдера*

*Показать какая скорость шара и какая скорость у ракетки*
*Привести в пример код, где это написано*

# Библиотеки
## Не забыть добавить в visual studio в расширениях monogame template extension
- Newtonsoft.Json _Используется для БД_ [ссылка](https://www.nuget.org/packages/Newtonsoft.Json)
- MonoGame.Desktop 3.5 _Используется для игрового движка_ [ссылка](https://www.nuget.org/packages/MonoGame.Framework.DesktopGL)
