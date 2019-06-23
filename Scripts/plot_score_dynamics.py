import matplotlib
import matplotlib.pyplot as plt
import requests


BLOCKCHAIN_URL = 'https://lambdacoin.org/lambda'


if __name__ == '__main__':
	public_id = '83'
	current_block = int(requests.get(f'{BLOCKCHAIN_URL}/getblockchaininfo').json()['block'])

	xs = range(3, current_block + 1)
	ys = []
	prev_balance = None
	for bn in xs:
		block_balance = int(requests.get(f'{BLOCKCHAIN_URL}/getblockinfo/{bn}').json()['balances'].get(public_id, 0))
		if prev_balance == block_balance:
			print(f'Block {bn - 1} was invalid')
		ys.append(block_balance)
		prev_balance = block_balance

	plt.plot(xs, ys)
	plt.savefig('balance.png')