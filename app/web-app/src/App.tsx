import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import CounterStore from './states/CounterStore'
import { observer } from 'mobx-react-lite'

const counterStore1 = new CounterStore();
const counterStore2 = new CounterStore();

const App = observer(() => {
  return (
    <>
      <div>
        <a href="https://vitejs.dev" target="_blank" rel="noreferrer">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank" rel="noreferrer">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>
      <h1>Vite + React</h1>
      <div className="card">
        <Counter counterStore={counterStore1} />
        <Counter counterStore={counterStore2} />
        <p>
          Edit <code>src/App.tsx</code> and save to test HMR
        </p>
      </div>
      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  )
})

const Counter = observer(({ counterStore }: { counterStore: CounterStore }) => {

  return (
    <div style={{padding: "10px"}}>
      <button onClick={() => counterStore.increment()}>
        count is {counterStore.counter}
      </button>
      <button onClick={() => counterStore.decrement()}>
        decrement
      </button>
    </div>
  );
});

export default App
