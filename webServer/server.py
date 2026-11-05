import asyncio
import websockets
import json
import random
import string

rooms = dict()

async def handler(websocket):
    wiadomosc = await websocket.recv()
    dane = json.loads(wiadomosc)
    print(dane)
    print("Ktoś dołączył!")
    mojKod = None
    if dane.get("akcja") == "host":
        kod = "".join(random.choices(string.ascii_uppercase, k=4))
        mojKod = kod
        rooms[kod] = [websocket]
        await websocket.send(json.dumps({"akcja": "twoj_kod", "kod": kod}))
    elif dane.get("akcja") == "join":
        podanyKod = dane.get("kod")
        if podanyKod in rooms:
            rooms[podanyKod].append(websocket)
            mojKod = podanyKod
            await websocket.send(json.dumps({"akcja": "polaczono"}))
        else:
            await websocket.send(json.dumps({"akcja": "blad"}))
    try:
        async for message in websocket:
            paczka = json.loads(message)
            paczka["nadawca"] = id(websocket)
            cel = paczka.get("odbiorca")
            for user in rooms[mojKod]:
                if user is not websocket:
                    if cel is None or str(id(user)) == str(cel):
                        await user.send(json.dumps(paczka))
            
    except:
        pass
    finally:
        if mojKod in rooms:
            rooms[mojKod].remove(websocket)
            if len(rooms[mojKod]) == 0:
                del rooms[mojKod]
        print("Ktoś wyszedł.")

async def main():
    async with websockets.serve(handler, "0.0.0.0", 8080):
        print("Serwer działa i czeka na połączenia...")
        await asyncio.Future()

if __name__ == "__main__":
    asyncio.run(main())