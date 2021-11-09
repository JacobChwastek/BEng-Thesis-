import {configureStore} from "@reduxjs/toolkit";
import {authSlice} from 'domain/auth/store/authSlice'
import {baseAPI as api} from 'infrastructure/persistance/api'


export const store = configureStore({
    reducer: {
        [api.reducerPath]: api.reducer,
        auth: authSlice.reducer
    },
});

export type RootState = ReturnType<typeof store.getState>;

export type AppDispatch = typeof store.dispatch;
