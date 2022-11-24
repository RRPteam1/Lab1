# Документация к проекту
Данные проект включает себя серверную и клиентскию часть игры Пинг-Понг написанную на сокетах.


# Оглавление

- **Глава I**

  - [Пакет (Packet)](#Пакет-(Packet))

- **Appendix**

  - [Библиотеки](#Библиотеки)


# Пакет (Packet)

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
## Типы пакетов (PacketType)

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
  - AcceptJoin
  - IsHereAck
  - GameStart
  - GameState
- **Клиентсие**
  - RequestJoin
  - IsHere
  - JoinAck
  - GameStartAck
  - PaddlePosition
- **Другие**
  - GameEnd
  
# Библиотеки
- Newtonsoft.Json
- MonoGame.Desktop 3.5
