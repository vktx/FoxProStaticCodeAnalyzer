# FoxPro-Static-Code-Analyzer
FoxPro Static Code Analyzer

# Возможности

- Поиск необъявленных локальных переменных
- Проверка на недопустимые символы в именах переменных (проверка на кириллицу)
- Проверка на повторное объявление локальных переменных
- Проверка на недопустимые инструкции в блоке обработки исключений (RETURN/RETRY)
- Проверка отсутствия ENDPROC/ENDFUNC
- Поиск недостижимого кода

# Использование 

    Analyzer.exe [параметры] <путь к папке с исходным кодом>
    
# Параметры

    iv (incorrect variable names) - проверять корректность имен переменных
    uv (undefined variables)      - искать необъявленные локальные переменные
    es (exception statement)      - проверять недопустимые инструкции в блоке обработки исключений
    uc (unreachable code)          - поиск недостижимого кода
    
Примечание:

    Если параметры не заданы используется полная проверка.
    Используте параметры только для задания нужных проверок.
    
# TODO ...

    1) Исключить из проверки присвоения системные переменные и объекты (_VFP, _SCREEN, APPLICATION, _TALLY, _CLIPTEXT и т.д.) возможно надо добавить файл с исключениями для глобальных переменных
    2) Некорректная обработка присвоения func("====10") воспринимается как func = 10
    3) Присвоение: создание переменной в функциях AFIELDS, ADIR, ANETRESOURCES, APRINTERS, AGETFILEVERSION, ADLLS, ADATABASES, AERROR, AMEMBERS, ASESSIONS, AUSED и т.д.
    4) Обработать LOCAL ARRAY
