using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Dapper;
using DBBasicApp.DAO;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Npgsql;

namespace DBBasicApp
{
    internal static class Program
    {
        private const string ConnectionString =
            "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=bdwork;Pooling=false;Connection Idle Lifetime=10;";

        private enum Option
        {
            UserAdd,
            ItemByType,
            Unknown
        }

        private static void Main()
        {
            Console.WriteLine("Добро пожаловать в CLI БД");
            Option input;
            while (true)
            {
                input = ChoiceDisplay();
                if (input == Option.Unknown) Console.WriteLine("Повторите ввод, неизвестный пункт меню");
                else break;
            }

            HandleOption(input);

            Console.WriteLine("Спасибо за использование, завершаюсь...");
        }

        private static void HandleOption(Option option)
        {
            switch (option)
            {
                case Option.UserAdd:
                    AddNewUser();
                    break;
                case Option.ItemByType:
                    AddItemByType();
                    break;
                default:
                    Console.WriteLine("Такой опции не существует");
                    break;
            }
        }

        private static Option ChoiceDisplay()
        {
            Console.WriteLine("Вы можете: \n1. Добавить нового пользователя\n2. Добавить предмет нужного типа\n" +
                              "Для выбора просто введите цифру нужного варианта и нажмите Enter");
            return Console.ReadLine() switch
            {
                "1" => Option.UserAdd,
                "2" => Option.ItemByType,
                _ => Option.Unknown
            };
        }

        private static void AddNewUser()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            var user = new UserEntity();
            while (true)
            {
                user.Username = ValueInput("username");
                user.Email = ValueInput("email");
                (user.PasswordHash, user.Salt) = GenerateHash(ValueInput("password"));
                try
                {
                    connection.Execute(
                        "INSERT INTO users(email,username,pwd_hash,salt) VALUES (@Email,@Username,@PasswordHash,@Salt)",
                        user);
                }
                catch (PostgresException e)
                {
                    Console.WriteLine($"У вас произошла ошибка! Текст ошибки: \n{e.MessageText}\nПопробуйте ещё раз!");
                    continue;
                }

                break;
            }

            connection.Close();
        }

        private static void AddItemByType()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            var item = new ItemEntity();
            var types = connection.Query<ItemTypeEntity>("SELECT * FROM types").ToArray();
            Console.WriteLine("Список всех имеющихся типов:");
            foreach (var type in types)
            {
                Console.WriteLine($"{type.type_id}. {type.Name}");
            }

            int typeId;
            while (true)
            {
                typeId = int.Parse(Input("Для выбора введите номер нужного типа"));
                if (types.Any(type => type.type_id == typeId))
                    break;
                Console.WriteLine("Нет такого номера! Попробуйте снова");
            }

            item.TypeId = typeId;
            while (true)
            {
                Console.WriteLine("Теперь, заполните значения нового предмета этого типа");
                item.Sprite = ValueInput("sprite");
                while (true)
                {
                    try
                    {
                        item.Price = int.Parse(ValueInput("Price"));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Это поле должно быть числом! Попробуйте ввод заново");
                        continue;
                    }

                    break;
                }
                
                while (true)
                {
                    Console.WriteLine("Введите да/нет для следующего поля");
                    var onSale = ValueInput("onSale");
                    switch (onSale.ToLower())
                    {
                        case "да":
                            item.OnSale = true;
                            break;
                        case "нет":
                            item.OnSale = false;
                            break;
                        default:
                            Console.WriteLine("Ввод не распознан! Помните, нужно ввести лишь Да или Нет");
                            continue;
                    }

                    break;
                }

                try
                {
                    connection.Execute(
                        "INSERT INTO item(sprite,type_id,price,on_sale) VALUES (@Sprite,@TypeId,@Price,@OnSale)",
                        item);
                }
                catch (PostgresException e)
                {
                    Console.WriteLine($"У вас произошла ошибка! Текст ошибки: \n{e.MessageText}\nПопробуйте ещё раз!");
                    continue;
                }

                connection.Close();
                break;
            }
        }

        private static KeyValuePair<string, string> GenerateHash(string password)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var stringSalt = Convert.ToBase64String(salt);
            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, 1000,
                256 / 8));
            return new KeyValuePair<string, string>(hash, stringSalt);
        }

        private static string Input(string s)
        {
            Console.WriteLine(s);
            return Console.ReadLine();
        }

        private static string ValueInput(string s)
        {
            return Input($"Введите значение для {s}");
        }
    }
}