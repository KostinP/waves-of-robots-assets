#!/bin/bash

# Выходной файл
output_file="combined_output.txt"

# Очищаем выходной файл
> "$output_file"

# Массив исключаемых файлов
excluded_files=("CardsStyles.uss" "CardsUI.uxml" "CommonStyles.uss" "HudStyles.uss" "HUDUI.uxml" "MainMenuStyles.uss" "PauseMenu.uxml")

# Функция для проверки исключения файла
is_excluded() {
    local filename=$1
    for excluded in "${excluded_files[@]}"; do
        if [[ "$filename" == "$excluded" ]]; then
            return 0
        fi
    done
    return 1
}

# Обрабатываем каждую директорию
for dir in "./Resources/Localization" "./Scripts" "./Scripts/UI" "./UI"; do
    if [ -d "$dir" ]; then
        echo "Обработка директории: $dir" >> "$output_file"
        echo "==========================================" >> "$output_file"
        
        # Ищем и обрабатываем файлы
        find "$dir" -type f \( -name "*.json" -o -name "*.cs" -o -name "*.uxml" -o -name "*.uss" \) | while read file; do
            filename=$(basename "$file")
            if ! is_excluded "$filename"; then
                echo "" >> "$output_file"
                echo "Файл: $file" >> "$output_file"
                cat "$file" >> "$output_file"
            fi
        done
        
        echo "" >> "$output_file"
    else
        echo "Директория $dir не существует, пропускаем" >> "$output_file"
    fi
done

echo "Готово! Результат сохранен в $output_file"