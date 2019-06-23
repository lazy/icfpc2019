import matplotlib
import matplotlib.pyplot as plt
import requests


BLOCKCHAIN_URL = 'https://lambdacoin.org/lambda'


if __name__ == '__main__':
	public_id = '83'
	current_block = int(requests.get(f'{BLOCKCHAIN_URL}/getblockchaininfo').json()['block'])

	xs = range(3, current_block + 1)
	ys = []
	places = []
	prev_balances = None
	for bn in xs:
		block_info = requests.get(f'{BLOCKCHAIN_URL}/getblockinfo/{bn}').json()

		balances = block_info['balances']
		block_balance = int(balances.get(public_id, 0))

		if prev_balances is not None:
			scoreboard = list(sorted([(player, balances[player] - prev_balances.get(player, 0)) for player in balances], key=lambda p: -p[1]))
			our_idx = [idx for idx in range(len(scoreboard)) if scoreboard[idx][0] == public_id]
			our_place = our_idx[0] + 1 if our_idx else 26
			places.append(our_place)

			if prev_balances.get(public_id) == block_balance:
				print(f'Block {bn - 1} was invalid')

		ys.append(block_balance)
		prev_balances = balances

	plt.plot(xs, ys)
	plt.savefig('balance.png')
	plt.clf()

	plt.plot(xs[:-1], places)
	plt.savefig('places.png')