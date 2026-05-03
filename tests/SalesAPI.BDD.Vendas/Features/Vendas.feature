#language: pt-br
Funcionalidade: Realizar Venda
  Como vendedor
  Quero registrar vendas para clientes
  Para que o estoque seja baixado e o histórico fique registrado

Contexto:
  Dado que existe o cliente "João" com email "joao@email.com" e cpf "11122233344"
  E que existe o produto "Mouse" com preço 50.00 e estoque 10

Cenário: Realizar uma venda válida reduz o estoque e calcula o total
  Quando o cliente "João" compra 3 unidades do produto "Mouse"
  Então a resposta deve ser status 201
  E o total da venda deve ser 150.00
  E o produto "Mouse" deve ter estoque 7

Cenário: Não permitir venda com estoque insuficiente
  Quando o cliente "João" compra 99 unidades do produto "Mouse"
  Então a resposta deve ser status 400
  E o produto "Mouse" deve ter estoque 10
  E a mensagem de erro deve conter "Estoque insuficiente"

Cenário: Não permitir venda para cliente inexistente
  Quando eu tento vender 1 unidade do produto "Mouse" para o cliente de id 9999
  Então a resposta deve ser status 400
  E a mensagem de erro deve conter "Cliente"
