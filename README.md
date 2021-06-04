# StealerChecker by Temnij
Инструмент для работы с логами Echelon
<br>
Запуск:<br>
```
StealerChecker
Copyright Temnij 2021

  Required option 'p, path' is missing.

  -p, --path          Required. Path to folder with logs

  -v, --verbose       Passwords view verbose mode

  -e, --everything    Use Everything service

  --help              Display this help screen.

  --version           Display version information.

```
![Скриншот](Image%201105.jpg)

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
