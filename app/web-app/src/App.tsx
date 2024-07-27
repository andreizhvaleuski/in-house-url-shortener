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
      <button onClick={() => counterStore.increment()} className="text-gray-900 bg-gradient-to-r from-teal-200 to-lime-200 hover:bg-gradient-to-l hover:from-teal-200 hover:to-lime-200 focus:ring-4 focus:outline-none focus:ring-lime-200 dark:focus:ring-teal-700 font-medium rounded-lg text-sm px-5 py-2.5 text-center me-2 mb-2">
        count is {counterStore.counter}
      </button>
      <button onClick={() => counterStore.decrement()} className="text-white bg-gradient-to-r from-red-400 via-red-500 to-red-600 hover:bg-gradient-to-br focus:ring-4 focus:outline-none focus:ring-red-300 dark:focus:ring-red-800 shadow-lg shadow-red-500/50 dark:shadow-lg dark:shadow-red-800/80 font-medium rounded-lg text-sm px-5 py-2.5 text-center me-2 mb-2">
        decrement
      </button>
    </div>
  );
});

export default App
