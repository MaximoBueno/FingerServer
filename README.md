# FingerServer (Servidor Web para Huellero ZKteco)
## ¿Qué es?
Es un servidor web que obtiene las huellas digitales, del biometrico ZKteco, y lo convierte a formato Base64.

## Uso:
Se puede consumir este servidor web mediante:
* Ajax/Jquery
* Otros

## Parametros de configuración (App.config):
* puerto: indica el puerto del servidor web (número).
* huellero: indica si el huellero va a funcionar (1=si,0=20).
* equipo: indica que conexion tiene ese equipo (0,1 o 2), generalmente es 0.

## Requisitos:
* Driver Zkteco para .net (master/FingerServer/Driver/)
* . Net Framework 4.7.2

## Probables mejoras:
* Agreagar al servidor un retorno de tipo JSON y quitar el retonor HTML
