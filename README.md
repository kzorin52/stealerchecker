# StealerChecker by Temnij
Инструмент для работы с логами Echelon, RedLine, Racoon, DCRat

# DONATES
**BNB**: `0x4DbCc2F2d98fe511d58413E7eD1ABaEBD527a785`, `bnb1k7nrwupe963fqazvqed2dhl3hkyqaus4mamwrq` <br>
**BTC**: `35RVYseJRa2sjv1MA8DthSHExZ9pjN1zGh` <br>
**ETH**: `0x51f9dbf8da18daf52824a56097c028ae7083b10c` <br>
**LTC**: `MMBdsMz3ragn5sHvExSkc7nJNK21a9yBmf` <br>
**DASH**: `XxJuYop48d9fGWQZftoGRCAbv6WMWM4Rrb` <br>
**RIPPLE**: tag `3048931473`, `rhn6BacbhRp7Q8McU7bbxTvLN4SHU5WGwn` <br>
**USDC** & **USDT** (erc20): `0x51f9dbf8da18daf52824a56097c028ae7083b10c` <br>
**DOGE**: `D9YafRhTirhnqwMXipN71bJW2A5JHczymw`<br> 
**TRX** & **USDT** (trc20): `TPuQCPKFMDPAvgVcFwMa2oKjDQyAXLve5k` <br>
**TON** (TON Coin): `UQDYm7tdhIf0mAaLT-AFbvAMBbSuY_KOOXrrDcJeIDUIFRdd` <br>
**SOL**: `GYci8PNe1Pocrhf4z1DtjcgXpwdKu97m9nwfrjFXesDk` <br>

<br>
Запуск:<br>

```
StealerChecker
Copyright Temnij 2022

  Required option 'p, path' is missing.

  -p, --path          Required. Path to folder with logs

  -v, --verbose       Passwords view verbose mode

  -e, --everything    Use Everything service

  --help              Display this help screen.

  --version           Display version information.

```
![Скриншот](https://github.com/kzorin52/stealerchecker/raw/master/Image%205.png)

# Базовые функции
* `Get CCs` - Вытаскивает все карточки из логов
![Скриншот](https://i.imgur.com/F4cw6kT.jpg)

* `Get FTPs` - Вытаскивает и **проверяет на валидность** все FTP-сервера из лога
![Скриншот](https://i.imgur.com/v6qPu8M.jpg)

* `Get Discord tokens` - Вытаскивает токены из всех сессий Discord, и записывает их в файл DiscordTokens.txt
![Скриншот](https://i.imgur.com/ig105Mk.jpg)

* `Search passwords` - Вытаскивает все пары логин:пароль по указанной части URL
![Скриншот](https://i.imgur.com/SVlyqmm.jpg)

* `Get Telegrams` - Вытаскивает все сессии Телеграм, с последующей возможностью открытия по номеру **[В ПАПКЕ С ПРОГРАММОЙ ДОЛЖЕН БЫТЬ TELEGAM PORTABLE С НАЗВАНИЕМ `Telegram.exe`]**
![Скриншот](https://i.imgur.com/SloDJJs.png)

* `Sort Logs by Date` - Сортирует логи, создаёт папку Sorts и папки с годами, потом в папках годами папки с месяцами, и сортитрует логи по датам

* `VERBOSE` - Данный параметр отвечает за то, чтобы при `Search passwords` видеть не только логин:пароль, но и полный URL сайта

![C Verbose](https://i.imgur.com/LyjNBUQ.png "С Verbose") <br>
_C Verbose_ <br> <br>

![БЕЗ Verbose](https://i.imgur.com/SVlyqmm.jpg "БЕЗ Verbose") <br>
_БЕЗ Verbose_
