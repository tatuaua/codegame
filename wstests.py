import asyncio
import websockets
import json

WEBSOCKET_URL = "ws://localhost:5000/ws"

def create_message(action, player_name, password, game_id=None, code=None):
    message = {"action": action, "player": {"name": player_name, "passWord": password}}
    if game_id:
        message["gameId"] = game_id
    if code:
        message["code"] = code
    return json.dumps(message)

async def send_messages():
    async with websockets.connect(WEBSOCKET_URL) as ws:
        # Create Game
        await ws.send(create_message("createGame", "player1", "pass"))
        response = json.loads(await ws.recv())
        game_id = response.get("gameId")
        print(f"Game created with ID: {game_id}")

        if not game_id:
            print("Failed to retrieve gameId. Exiting.")
            return

        # Find Game
        await ws.send(create_message("findGame", "player2", "pass2"))
        print("Sent findGame request.")
        print(await ws.recv())

        # Join Game
        await ws.send(create_message("joinGame", "player2", "pass2", game_id))
        print("Sent joinGame request.")
        print(await ws.recv())

        # Send Bug Report
        bug_code = "some code\nasd lines\naaaa"
        await ws.send(create_message("bug", "player1", "pass", game_id, bug_code))
        print("Sent bug.")
        print(await ws.recv())

        # Fix Bug
        fix_code = "some code\nsd lines\naaaa"
        await ws.send(create_message("fix", "player2", "pass2", game_id, fix_code))
        print("Sent fix.")

        await ws.send(create_message("fakeAction", "player2", "pass2"))
        print("Sent invalid action.")
        message = await ws.recv()
        assert isinstance(message, str) and "error" in message, f"Unexpected message received: {message}"

asyncio.run(send_messages())
