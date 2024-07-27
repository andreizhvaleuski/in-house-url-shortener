import { makeAutoObservable } from "mobx"

class CounterStore {
    counter = 0;

    constructor() {
        makeAutoObservable(this);
    }

    increment() {
        this.counter += 1;
    }

    decrement() {
        this.counter -= 1;
    }
}

export default CounterStore;
