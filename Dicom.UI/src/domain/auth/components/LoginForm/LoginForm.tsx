import * as React from "react";
import {useHistory} from "react-router-dom";
import {Button, Card, Form} from "antd";
import {useDispatch} from "react-redux";

import {useLoginMutation, useUserMutation} from 'domain/auth/store/api';
import {setLogIn, setUser} from 'domain/auth/store/authSlice'

import Logo from "ui/assets/img/logo.png";
import {RotatingLogo} from "ui/Logo/RotatingLogo";
import {Space} from "ui/Space/Space";
import {Title} from "ui/Typography/Title/Title";
import {Input, InputPassword} from "ui/Input/Input";
import {SubmitButton} from "ui/Button/SubmitButton";
import {UserOutlined, LockOutlined} from "@ant-design/icons";

import "./LoginForm.scss";

type Props = {
    registerPageUrl: string;
};

export function LoginForm({registerPageUrl}: Props) {
    const dispatch = useDispatch()
    const history = useHistory();

    const [login, {isLoading}] = useLoginMutation();
    const [getUser, {}] = useUserMutation();

    const onFinish = async (values: any) => {
        try {
            const result = await login(values).unwrap();
            dispatch(setLogIn(result));
            const { user } = await getUser().unwrap() || {};
            dispatch(setUser(user))
        } catch (e) {
            console.log(e)
        } finally {

        }
    };

    const onFinishFailed = (errorInfo: any) => {
        console.log("Failed:", errorInfo);
    };

    const onRedirectToRegisterClick = () => {
        history.push(registerPageUrl);
    };

    return (
        <Card
            className="login"
            title={
                <Space align="center" direction="column">
                    <RotatingLogo redirectTo="/" src={Logo} alt="logo"/>
                    <Title>Login</Title>
                </Space>
            }
        >
            <Form
                name="login"
                className="login__form"
                labelCol={{span: 8}}
                wrapperCol={{span: 16}}
                onFinish={onFinish}
                onFinishFailed={onFinishFailed}
                layout="vertical"
                autoComplete="off"
            >
                <Form.Item className="login__item" name="email"
                           rules={[{required: true, message: "Please input your username!"}]}>
                    <Input prefix={<UserOutlined/>} label="Email" type="outline"/>
                </Form.Item>

                <Form.Item className="login__item" name="password"
                           rules={[{required: true, message: "Please input your password!"}]}>
                    <InputPassword prefix={<LockOutlined/>} label="Hasło" type="outline"/>
                </Form.Item>

                <Space direction="column" justify="center">
                    <Form.Item className="login__item">
                        <SubmitButton onClick={() => {
                        }}>Login</SubmitButton>
                    </Form.Item>
                    <Form.Item className="login__item">
                        <Button onClick={onRedirectToRegisterClick} type="link">
                            Nie posiadasz konta? Zarejestruj się teraz
                        </Button>
                    </Form.Item>
                </Space>
            </Form>
        </Card>
    );
}
