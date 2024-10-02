import requests

url = 'http://localhost/api/users'

# Установка заголовков
headers = {
    'Accept': '*/*',
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': '0'  # Это не обязательно включать, он будет автоматически обработан
}

# Отправка POST запроса
response = requests.post(url, headers=headers)

# Вывод ответа
print(f'Status Code: {response.status_code}')
print(f'Response Body: {response.text}')
